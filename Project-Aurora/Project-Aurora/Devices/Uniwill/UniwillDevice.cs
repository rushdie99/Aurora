﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aurora.Devices.RGBNet;
using Aurora.Settings;
using Microsoft.Win32;
using UniwillSDKDLL;

namespace Aurora.Devices.Uniwill
{
     
    enum GAMECENTERTYPE
    {
        NONE = 0,
        GAMINGTCENTER = 1,
        CONTROLCENTER = 2
        
    }


    public class UniwillDevice : Device
    {
        
       // Generic Variables
        private string devicename = "keyboard";
        private bool isInitialized = false;

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private long lastUpdateTime = 0;

        System.Timers.Timer regTimer;
        const string Root = "HKEY_LOCAL_MACHINE";
        const string subkey = @"SOFTWARE\OEM\Aurora";
        const string keyName = Root + "\\" + subkey;
        int SwitchOn = 0;
        List<AuroraInterface> DeviceList = new List<AuroraInterface>();
     
        private AuroraInterface keyboard = null;
     
        System.Timers.Timer _CheckControlCenter = new System.Timers.Timer();

        GAMECENTERTYPE GamingCenterType = 0;
     
        float brightness = 1f;
        public UniwillDevice()
        {
            devicename = KeyboardFactory.GetOEMName();
            ChoiceGamingCenter();
 
        }
 

        private void ChoiceGamingCenter()
        {
            GamingCenterType = CheckGC();
             
            if (GamingCenterType == GAMECENTERTYPE.GAMINGTCENTER)
            {
                regTimer = new System.Timers.Timer();
                regTimer.Interval = 1000;
                regTimer.Elapsed += OnRegChanged;
                regTimer.Start();
     
            }
        }

      

        private GAMECENTERTYPE CheckGC()
        {
            try
            {
                int Control = (int)Registry.GetValue(keyName, "AuroraSwitch", 0);
                if(Control==1)
                {
                    GamingCenterType = GAMECENTERTYPE.GAMINGTCENTER;
                    SwitchOn = Control;
                }
           
            }
            catch (Exception ex)
            {
                GamingCenterType = GAMECENTERTYPE.NONE;
                SwitchOn = 0;
            }
            return GamingCenterType;
        }
 

        public bool CheckGCPower()
        {
           if(GamingCenterType== GAMECENTERTYPE.GAMINGTCENTER)
           {
                int Control = (int)Registry.GetValue(keyName, "AuroraSwitch", 0);
                if (Control == 0)
                    return false;
                else
                    return true;
            }
            
            else 
              return true;
        }

       
        private void OnRegChanged(object sender, EventArgs e)
        {
            int newSwtich =(int) Registry.GetValue(keyName, "AuroraSwitch", 0);
            if(SwitchOn != newSwtich)
            {
                SwitchOn = newSwtich;
                if (CheckGCPower())
                {
                    Initialize();
                }
                else
                {
                    bRefreshOnce = true;
                    isInitialized = false;
                    Shutdown();

                }
               
            }
            

        }

        public string GetDeviceName()
            {
                return devicename;
            }

            public string GetDeviceDetails()
            {
                if (isInitialized)
                {
                    return devicename + ": Initialized";
                }
                else
                {
                    return devicename + ": Not initialized";
                }
            }

            public bool Initialize()
            {
           

              if (!isInitialized && CheckGCPower())
                {
                try
                {
                    keyboard =   KeyboardFactory.CreateHIDDevice("hidkeyboard");
                        if (keyboard!=null)
                        {
                            bRefreshOnce = true;
                            isInitialized = true;
                     
                            return true;
                        }
                           
                        isInitialized = false;
                        return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Uniwill device error!");
                }
                // Mark Initialized = FALSE
                isInitialized = false;
                return false;
            }
 

            return isInitialized;

            }
 
            public void Shutdown()
            {
                if (this.IsInitialized())
                {
                    if(CheckGCPower())
                     keyboard?.release();

                    bRefreshOnce = true;
                    isInitialized = false;
                   
                }
            }

            public void Reset()
            {
                if (this.IsInitialized())
                {
                 if(CheckGCPower())
                    keyboard?.release();

                 bRefreshOnce = true;
                 isInitialized = false;
                
                }
                   
            }

            public bool Reconnect()
            {
                throw new NotImplementedException();
            }

            public bool IsInitialized()
            {
        
               return isInitialized;
            }

            public bool IsConnected()
            {
                 return isInitialized;
           }
        bool bRefreshOnce = true; // This is used to refresh effect between Row-Type and Fw-Type change or layout light level change
        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)  
        {
            if (e.Cancel) return false;

            bool update_result = false;

            watch.Restart();

           
            //Alpha necessary for Global Brightness modifier
            var adjustedColors = keyColors.Select(kc => AdjustBrightness(kc));

            keyboard?.SetEffect(0x32, 0x00, bRefreshOnce, adjustedColors, e);

            bRefreshOnce = false;
       
            watch.Stop();

            lastUpdateTime = watch.ElapsedMilliseconds;

            return update_result;

        }
 

        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false) => UpdateDevice(colorComposition.keyColors, e, forced);

        private KeyValuePair<DeviceKeys, Color> AdjustBrightness(KeyValuePair<DeviceKeys, Color> kc)
        {
            var newEntry = new KeyValuePair<DeviceKeys, Color>(kc.Key, System.Drawing.Color.FromArgb(255, Utils.ColorUtils.MultiplyColorByScalar(kc.Value, (kc.Value.A / 255.0D) * brightness)));
            kc= newEntry;
            return kc;
        }


        // Device Status Methods
        public bool IsKeyboardConnected()
            {
                return isInitialized;
            }

            public bool IsPeripheralConnected()
            {
                return isInitialized;
            }

            public string GetDeviceUpdatePerformance()
            {
                return (isInitialized ? lastUpdateTime + " ms" : "");
            }

            public VariableRegistry GetRegisteredVariables()
            {
                return new VariableRegistry();
            }
        }
   
}
