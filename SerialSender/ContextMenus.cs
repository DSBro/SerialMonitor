﻿using System;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;
using SerialSender.Properties;
using System.Drawing;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using System.Timers;
using System.Net.NetworkInformation;

namespace SerialSender
{

    class ContextMenus
    {

        SerialPort SelectedSerialPort;
        ContextMenuStrip menu;
        OpenHardwareMonitor.Hardware.Computer thisComputer;
        private class StateObjClass
        {

            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;
        }
        public ContextMenuStrip Create()
        {
           
            thisComputer = new OpenHardwareMonitor.Hardware.Computer() { };
            thisComputer.CPUEnabled = true;
            thisComputer.GPUEnabled = true;
            thisComputer.HDDEnabled = true;
            thisComputer.MainboardEnabled = true;
            thisComputer.RAMEnabled = true;
            thisComputer.Open();
            

            menu = new ContextMenuStrip();
            CreateMenuItems();
            return menu;
        }

        void CreateMenuItems()
        {
           
            ToolStripMenuItem item;
            ToolStripSeparator sep;


            item = new ToolStripMenuItem();
            item.Text = "Serial Ports";
            menu.Items.Add(item);

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                item = new ToolStripMenuItem();
                item.Text = port;
                item.Click += new EventHandler((sender, e) => Selected_Serial(sender, e, port));
                item.Image = Resources.Serial;
                menu.Items.Add(item);
            }


            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem();
            item.Text = "Refresh";
            item.Click += new EventHandler( (sender, e ) => InvalidateMenu(menu) );
            //item.Image = Resources.Exit;
            menu.Items.Add(item);

            sep = new ToolStripSeparator();
            menu.Items.Add(sep);

            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new System.EventHandler(Exit_Click);
            item.Image = Resources.Exit;
            menu.Items.Add(item);

        }

        void InvalidateMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();
            CreateMenuItems();
        }

        void Selected_Serial(object sender, EventArgs e, string selected_port)
        {
            Console.WriteLine("Selected port");
            Console.WriteLine(selected_port);
            Console.ReadLine();
            SelectedSerialPort = new SerialPort(selected_port);
            if ( ! SelectedSerialPort.IsOpen)
            {
                SelectedSerialPort.Open();

            };
            StateObjClass StateObj = new StateObjClass();
            StateObj.TimerCanceled = false;
            System.Threading.TimerCallback TimerDelegate = new System.Threading.TimerCallback(dataCheck);
            System.Threading.Timer TimerItem = new System.Threading.Timer(TimerDelegate, StateObj, 1000, 3000);
            StateObj.TimerReference = TimerItem;
            // SelectedSerialPort.WriteLine("Send Data \n");
        }


        private void dataCheck(object StateObj)
        {
            string cpuTemp ="";
            string gpuTemp = "";
            string gpuLoad = "";
            string cpuLoad="";
            string ramUsed = "";
;
            StateObjClass State = (StateObjClass)StateObj;
            // enumerating all the hardware
            foreach (OpenHardwareMonitor.Hardware.IHardware hw in thisComputer.Hardware)
            {
                Console.WriteLine("Checking: " + hw.HardwareType);
                Console.ReadLine();
                
                hw.Update();
                // searching for all sensors and adding data to listbox
                foreach (OpenHardwareMonitor.Hardware.ISensor s in hw.Sensors)
                {
                    Console.WriteLine("Sensor: " + s.Name + " Type: " + s.SensorType + " Value: " + s.Value);
                    Console.ReadLine();
                  
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature)
                    {
                        if (s.Value != null)
                        {
                            int curTemp = (int)s.Value;
                            switch (s.Name)
                            {
                                case "CPU Package":
                                    cpuTemp = curTemp.ToString();
                                    break;
                                case "GPU Core":
                                    gpuTemp = curTemp.ToString();
                                    break;

                            }
                        }  
                    }
                    if ( s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Load)
                    {
                        if ( s.Value != null)
                        {
                            int curLoad = (int)s.Value;
                            switch (s.Name)
                            {
                                case "CPU Total":
                                    cpuLoad = curLoad.ToString();
                                    break;
                                case "GPU Core":
                                    gpuLoad = curLoad.ToString();
                                    break;
                            }
                        }
                    }
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Data)
                    {
                        if ( s.Value != null)
                        {
                            switch(s.Name)
                            {
                                case "Used Memory":
                                    decimal decimalRam = Math.Round((decimal)s.Value, 1);
                                    ramUsed = decimalRam.ToString();
                                    break;
                            }
                        }
                    }
                }
            }
            string curSong = "";
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {

                    if ( process.ProcessName == "AIMP3")
                    {
                        curSong = process.MainWindowTitle;
                    } else if ( process.ProcessName == "foobar2000" && (process.MainWindowTitle.IndexOf("[") > 0 ) )
                    {
                        curSong = process.MainWindowTitle.Substring(0, process.MainWindowTitle.IndexOf("[")-1);
                    }
                }
            }
            string arduinoData = "C" + cpuTemp + "c " + cpuLoad + "%|G" + gpuTemp +"c " + gpuLoad + "%|R"+ ramUsed +"G|S" + curSong + "|";
            SelectedSerialPort.WriteLine(arduinoData);

        }
        void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}