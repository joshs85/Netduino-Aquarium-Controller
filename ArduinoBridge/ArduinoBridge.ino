#include <dht11.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <Wire.h>
#include <SerialCommand.h>

//Declare objects
dht11 AirTempSensor;
SerialCommand sCmd;

//Settings for DHT11 Sensor
#define AIR_TEMP_PIN 2
//Setting for CO2 Sensor
#define MG_PIN (7)     //define which analog input channel you are going to use
#define BOOL_PIN (3)
#define DC_GAIN (8.5)   //define the DC gain of amplifier
#define READ_SAMPLE_INTERVAL (50)    //define how many samples you are going to take in normal operation
#define READ_SAMPLE_TIMES (5)     //define the time interval(in milisecond) between each samples in normal operation
#define ZERO_POINT_VOLTAGE (0.220) //define the output of the sensor in volts when the concentration of CO2 is 400PPM
#define REACTION_VOLTGAE (0.020) //define the voltage drop of the sensor when move the sensor from air into 1000ppm CO2
float CO2Curve[3]  =  {2.602,ZERO_POINT_VOLTAGE,(REACTION_VOLTGAE/(2.602-3))};   
                                                     //two points are taken from the curve. 
                                                     //with these two points, a line is formed which is
                                                     //"approximately equivalent" to the original curve.
                                                     //data format:{ x, y, slope}; point1: (lg400, 0.324), point2: (lg4000, 0.280) 
                                                     //slope = ( reaction voltage ) / (log400 ¨Clog1000) 

// Variable to store temperature and humidity readings
float air_temp_c;
float humidity;

void setup()
{ 
  // Start the serial port to communicate to the PC at 115200 baud
  Serial.begin(57600);
  sCmd.addCommand("R", GetDHTReadings);
  sCmd.addCommand("CO2",GetCO2Reading);
  
  pinMode(BOOL_PIN, INPUT); //set pin to input
  digitalWrite(BOOL_PIN, HIGH); //turn on pullup resistors
  
  delay(1000);
}

void loop()
{ 
  //Check for Serial Input
  sCmd.readSerial();
}

void GetDHTReadings()
{
  int chk = AirTempSensor.read(AIR_TEMP_PIN);
  humidity = AirTempSensor.humidity;
  air_temp_c = AirTempSensor.temperature;
  Serial.print(air_temp_c);
  Serial.print(",");
  Serial.print(humidity);
  Serial.print('\r');
}

void GetCO2Reading()
{
    int percentage;
    float volts;
    volts = MGRead(MG_PIN);
    //Serial.print( "SEN-00007:" );
    //Serial.print(volts); 
    //Serial.print( "V           " );
    
    percentage = MGGetPercentage(volts,CO2Curve);
    Serial.print(percentage);
    Serial.print("\r");
}

/*****************************  MGRead *********************************************
Input:   mg_pin - analog channel
Output:  output of SEN-000007
Remarks: This function reads the output of SEN-000007
************************************************************************************/ 
float MGRead(int mg_pin)
{
    int i;
    float v=0;

    for (i=0;i<READ_SAMPLE_TIMES;i++) {
        v += analogRead(mg_pin);
        delay(READ_SAMPLE_INTERVAL);
    }
    v = (v/READ_SAMPLE_TIMES) *5/1024 ;
    return v;  
}

/*****************************  MQGetPercentage **********************************
Input:   volts   - SEN-000007 output measured in volts
         pcurve  - pointer to the curve of the target gas
Output:  ppm of the target gas
Remarks: By using the slope and a point of the line. The x(logarithmic value of ppm) 
         of the line could be derived if y(MG-811 output) is provided. As it is a 
         logarithmic coordinate, power of 10 is used to convert the result to non-logarithmic 
         value.
************************************************************************************/ 
int  MGGetPercentage(float volts, float *pcurve)
{
   if ((volts/DC_GAIN )>=ZERO_POINT_VOLTAGE) {
      return -1;
   } else { 
      return pow(10, ((volts/DC_GAIN)-pcurve[1])/pcurve[2]+pcurve[0]);
   }
}
