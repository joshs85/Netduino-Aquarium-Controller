﻿using System;
using System.Collections;
using Controller;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Plugins
{
	/// <summary>
	/// Control Plugin to manage the RelayShield http://www.seeedstudio.com/depot/relay-shield-p-693.html?cPath=132_134
	/// Used to control lighting and ATO solenoid
	/// </summary>
	public class Relays : ControlPlugin
	{
		~Relays() { Dispose(); }
		public override void Dispose() { m_relayPins = null; m_commands = null; }

		private Hashtable m_commands;
		public override Hashtable Commands() { return m_commands; }

		public override string WebFragment
		{
			get { return "relays.html"; }
		}

		/// <summary>
		/// Used during Command parsing to trigger immediate command execution.
		/// Example: during mid-day, the lights should be on, but a reboot of the controller will restart
		/// the Netduino, forcing a re-parse of the ini file.  
		/// The timers will be setup to turn on the next day, but they need to be on immediately as well
		/// </summary>
		private bool m_missedExecute = false;

		private OutputPort[] m_relayPins;
		public Relays(object _config)
		{
			// Initialize Relay Pin controls
			m_relayPins = new OutputPort[4];
			m_relayPins[0] = new OutputPort(Pins.GPIO_PIN_D7, false);
			m_relayPins[1] = new OutputPort(Pins.GPIO_PIN_D6, false);
			m_relayPins[2] = new OutputPort(Pins.GPIO_PIN_D5, false);
			m_relayPins[3] = new OutputPort(Pins.GPIO_PIN_D4, false);

			// parse config Hashtable and store array list of commands
			// the individual relays are stored in an Array List
			m_commands = new Hashtable();
			Hashtable config = (Hashtable)_config;			
			ArrayList relays = config["relays"] as ArrayList;
			ParseCommands(relays);
		}

		/// <summary>
		/// Execute RelayCommand given in state object
		/// </summary>
		/// <param name="state">RelayCommand object to process on execution</param>
		public override void ExecuteControl(object state)
		{
			// Callback received a RelayCommand struct to execute			
			var command = (RelayCommand)state;
			m_relayPins[command.relay].Write(command.status);			
		}

		/// <summary>
		/// Parse JSON array of relay config and store for delayed execution
		/// </summary>
		/// <param name="_commands">JSON formatted list of objects</param>
		private void ParseCommands(ArrayList _commands)
		{			
			foreach (Hashtable command in _commands)
			{
				// parse out details from config
				short relayID = Convert.ToInt16(command["id"].ToString());							
				TimeSpan timeOn = GetTimeSpan(command["on"].ToString());				
				RelayCommand relayOn = new RelayCommand(relayID, true);
				m_commands.Add(timeOn, relayOn);
				if (m_missedExecute)
					ExecuteControl(relayOn);
				
				TimeSpan timeOff = GetTimeSpan(command["off"].ToString());
				RelayCommand relayOff = new RelayCommand(relayID, false);
				m_commands.Add(timeOff, relayOff);
				if (m_missedExecute)
					ExecuteControl(relayOff);

			}			
		}

		/// <summary>
		/// Determine the timespan from 'now' when the given command should be run
		/// </summary>
		/// <param name="_time">An ISO8601 formatted time value representing the time of day for execution</param>
		/// <returns>Timespan when task should be run</returns>		
		private TimeSpan GetTimeSpan(string _time)
		{
			// Determine when this event should be fired as a TimeSpan
			// Assuming ISO8601 time format "hh:mm"
			int hours = int.Parse(_time.Substring(0, 2));
			int minutes = int.Parse(_time.Substring(3, 2));
			DateTime now = DateTime.Now;
			DateTime timeToRun = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

			// we missed the window, so setup for next run
			// Also, mark that we missed the run, so we can execute the command during the parse.
			
			if (timeToRun < now)
			{
				timeToRun = timeToRun.AddDays(1);				
				m_missedExecute = true;
			}

			return new TimeSpan((timeToRun - now).Ticks);
		}		
	}
}