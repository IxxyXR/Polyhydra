//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace LookingGlass {
	public static class Plugin {
		
		// calibration functions
		[DllImport("HoloPlay")] 
		public static extern int LoadCalibrations();
		[DllImport("HoloPlay")] 
		public static extern int CalibrationCount();
		[DllImport("HoloPlay")] 
		public static extern int GetModel(int cal);
		[DllImport("HoloPlay")] 
		public static extern int GetScreenWidth(int cal);
		[DllImport("HoloPlay")] 
		public static extern int GetScreenHeight(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetDPI(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetViewCone(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetAspect(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetPitch(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetSlope(int cal);
		[DllImport("HoloPlay")] 
		public static extern float GetCenter(int cal);
		[DllImport("HoloPlay")] 
		public static extern int GetSerial(int cal, byte[] output);
		[DllImport("HoloPlay")] 
		public static extern int GetLKGName(int cal, byte[] output);
		// for the hackers
		[DllImport("HoloPlay")] 
		public static extern int ReadCalibrations(byte[] output);

		// monitor functions
        [DllImport("HoloPlay")]
        public static extern int PopulateLKGDisplays();
        [DllImport("HoloPlay")]
        public static extern int GetLKGcount();
        [DllImport("HoloPlay")]
        public static extern int GetLKGcalIndex(int i);
        [DllImport("HoloPlay")]
        public static extern int GetLKGunityIndex(int i);
        [DllImport("HoloPlay")]
        public static extern int GetLKGxpos(int i);
        [DllImport("HoloPlay")]
        public static extern int GetLKGypos(int i);
        [DllImport("HoloPlay")]
        public static extern int GetLKGdisplayName(int i, byte[] output);

		public static LoadResults GetLoadResults(int i) {
			LoadResults results = new LoadResults();
			results.attempted = true;
			// bit 0: loaded from serial flash
			// bit 1: loaded from local storage
			// if either are true, calibration found is true
			results.calibrationFound = (i & 3) != 0;
			// bit 2: lkg display found
			results.lkgDisplayFound = (i & 4) != 0;
			return results;
		}
	}

	public struct Calibration {
		// set the index for which calibration this is
		public int index;
		public Calibration(int index) {
			this.index = index;
		}
		// helper functions to easily get calibration values
		public int model { get{ return Plugin.GetModel(index); } }
		public int screenWidth { get{ return Plugin.GetScreenWidth(index); } }
		public int screenHeight { get{ return Plugin.GetScreenHeight(index); } }
		public float dpi { get{ return Plugin.GetDPI(index); } }
		public float viewCone { get{ return Plugin.GetViewCone(index); } }
		public float aspect { get{ return Plugin.GetAspect(index); } }
		public float pitch { get{ return Plugin.GetPitch(index); } }
		public float slope { get{ return Plugin.GetSlope(index); } }
		public float center { get{ return Plugin.GetCenter(index); } }
		public string GetSerial() {
			byte[] output = new byte[128];
			Plugin.GetSerial(index, output);
			return System.Text.Encoding.ASCII.GetString(output).TrimEnd('\0');
		}	
		public string GetLKGName() {
			byte[] output = new byte[128];
			Plugin.GetLKGName(index, output);
			return System.Text.Encoding.ASCII.GetString(output).TrimEnd('\0');
		}	
	}

	public struct LoadResults {
		public bool attempted;
		public bool calibrationFound;
		public bool lkgDisplayFound;
		public LoadResults(bool attempted, bool calibrationFound, bool lkgDisplayFound) {
			this.attempted = attempted;
			this.calibrationFound = calibrationFound;
			this.lkgDisplayFound = lkgDisplayFound;
		}
	}

	[System.Serializable] 
	public class LoadEvent : UnityEvent<LoadResults> {};
}