﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ListenMoeClient
{
	enum Setting
	{
		//UI and form settings
		LocationX,
		LocationY,
		TopMost,
		SizeX,
		SizeY,
		FormOpacity,
		CustomColors,
		JPOPBaseColor,
		JPOPAccentColor,
		KPOPBaseColor,
		KPOPAccentColor,
		CustomBaseColor,
		CustomAccentColor,
		Scale,
		CloseToTray,
		HideFromAltTab,
		ThumbnailButton,

		//Visualiser settings
		EnableVisualiser,
		VisualiserResolutionFactor,
		FftSize,
		VisualiserBarWidth,
		VisualiserOpacity,
		VisualiserBars,
		VisualiserFadeEdges,
		JPOPVisualiserColor,
		KPOPVisualiserColor,
		CustomVisualiserColor,

		//Stream
		StreamType,

		//Misc
		UpdateAutocheck,
		UpdateInterval,
		Volume,
		OutputDeviceGuid,
		Token,
		Username,
		DiscordPresence
	}

	enum StreamType
	{
		Jpop,
		Kpop
	}

	//I should have just used a json serialiser
	static class Settings
	{
		public const int DEFAULT_WIDTH = 512;
		public const int DEFAULT_HEIGHT = 64;
		public const int DEFAULT_RIGHT_PANEL_WIDTH = 64;
		public const int DEFAULT_PLAY_PAUSE_SIZE = 20;

		private const string settingsFileLocation = "listenMoeSettings.ini";

		static readonly object settingsMutex = new object();
		static readonly object fileMutex = new object();

		static Dictionary<Type, object> typedSettings = new Dictionary<Type, object>();
		static readonly Dictionary<char, Type> typePrefixes = new Dictionary<char, Type>()
		{
			{ 'i', typeof(int) },
			{ 'f', typeof(float) },
			{ 'b', typeof(bool) },
			{ 's', typeof(string) },
			{ 'c', typeof(Color) },
			{ 't', typeof(StreamType) }
		};
		static readonly Dictionary<Type, char> reverseTypePrefixes = new Dictionary<Type, char>()
		{
			{ typeof(int), 'i'},
			{ typeof(float), 'f'},
			{ typeof(bool), 'b'},
			{ typeof(string), 's'},
			{ typeof(Color), 'c' },
			{ typeof(StreamType), 't' }
		};

		//Deserialisation
		static readonly Dictionary<Type, Func<string, (bool Success, object Result)>> parseActions = new Dictionary<Type, Func<string, (bool, object)>>()
		{
			{ typeof(int), s => {
				bool success = int.TryParse(s, out int i);
				return (success, i);
			}},
			{ typeof(float), s => {
				bool success = float.TryParse(s, out float f);
				return (success, f);
			}},
			{ typeof(bool), s => {
				bool success = bool.TryParse(s, out bool b);
				return (success, b);
			}},
			{ typeof(string), s => {
				return (true, s);
			}},
			{ typeof(Color), s => {
				if (int.TryParse(s.Replace("#", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int argb))
					return (true, Color.FromArgb(255, Color.FromArgb(argb)));
				else
					throw new Exception("Could not parse color '" + s + "'. Check your settings file for any errors.");
			}},
			{ typeof(StreamType), s => {
				if (s == "jpop")
					return (true, StreamType.Jpop);
				else if (s == "kpop")
					return (true, StreamType.Kpop);
				throw new Exception("Could not parse StreamType.");
			}}
		};

		//Serialisation
		static readonly Dictionary<Type, Func<dynamic, string>> saveActions = new Dictionary<Type, Func<dynamic, string>>()
		{
			{ typeof(int), i => i.ToString() },
			{ typeof(float), f => f.ToString() },
			{ typeof(bool), b => b.ToString() },
			{ typeof(string), s => s },
			{ typeof(Color), c => ("#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")).ToLowerInvariant() },
			{ typeof(StreamType), st => st == StreamType.Jpop ? "jpop" : "kpop" }
		};

		static Settings() => LoadDefaultSettings();

		public static T Get<T>(Setting key)
		{
			lock (settingsMutex)
			{
				return ((Dictionary<Setting, T>)(typedSettings[typeof(T)]))[key];
			}
		}

		public static void Set<T>(Setting key, T value)
		{
			Type t = typeof(T);
			lock (settingsMutex)
			{
				if (!typedSettings.ContainsKey(t))
				{
					typedSettings.Add(t, new Dictionary<Setting, T>());
				}
				((Dictionary<Setting, T>)typedSettings[t])[key] = value;
			}
		}

		private static void LoadDefaultSettings()
		{
			Set(Setting.LocationX, 100);
			Set(Setting.LocationY, 100);
			Set(Setting.VisualiserResolutionFactor, 3);
			Set(Setting.UpdateInterval, 3600); //in seconds
			Set(Setting.SizeX, DEFAULT_WIDTH);
			Set(Setting.SizeY, DEFAULT_HEIGHT);
			Set(Setting.FftSize, 2048);

			Set(Setting.Volume, 0.3f);
			Set(Setting.VisualiserBarWidth, 3.0f);
			Set(Setting.VisualiserOpacity, 0.5f);
			Set(Setting.FormOpacity, 1.0f);
			Set(Setting.Scale, 1.0f);

			Set(Setting.TopMost, false);
			Set(Setting.UpdateAutocheck, true);
			Set(Setting.CloseToTray, false);
			Set(Setting.HideFromAltTab, false);
			Set(Setting.ThumbnailButton, true);
			Set(Setting.EnableVisualiser, true);
			Set(Setting.VisualiserBars, true);
			Set(Setting.VisualiserFadeEdges, false);

			Set(Setting.Token, "");
			Set(Setting.Username, "");
			Set(Setting.OutputDeviceGuid, "");

			Set(Setting.JPOPVisualiserColor, Color.FromArgb(255, 1, 91));
			Set(Setting.JPOPBaseColor, Color.FromArgb(33, 35, 48));
			Set(Setting.JPOPAccentColor, Color.FromArgb(255, 1, 91));

			Set(Setting.KPOPVisualiserColor, Color.FromArgb(48, 169, 237));
			Set(Setting.KPOPBaseColor, Color.FromArgb(33, 35, 48));
			Set(Setting.KPOPAccentColor, Color.FromArgb(48, 169, 237));

			Set(Setting.CustomColors, false);
			Set(Setting.CustomVisualiserColor, Color.FromArgb(255, 1, 91));
			Set(Setting.CustomBaseColor, Color.FromArgb(33, 35, 48));
			Set(Setting.CustomAccentColor, Color.FromArgb(255, 1, 91));

			Set(Setting.StreamType, StreamType.Jpop);
			Set(Setting.DiscordPresence, true);
		}

		public static void LoadSettings()
		{
			if (!File.Exists(settingsFileLocation))
			{
				WriteSettings();
				return;
			}

			string[] lines = File.ReadAllLines(settingsFileLocation);
			foreach (string line in lines)
			{
				string[] parts = line.Split(new char[] { '=' }, 2);
				if (string.IsNullOrWhiteSpace(parts[0]))
					continue;

				char prefix = parts[0][0];
				Type t = typePrefixes[prefix];
				Func<string, (bool Success, object Result)> parseAction = parseActions[t];
				(bool success, object o) = parseAction(parts[1]);
				if (!success)
					continue;

				if (!Enum.TryParse(parts[0].Substring(1), out Setting settingKey))
					continue;

				MethodInfo setMethod = typeof(Settings).GetMethod("Set", BindingFlags.Static | BindingFlags.Public);
				MethodInfo genericSet = setMethod.MakeGenericMethod(t);
				genericSet.Invoke(null, new object[] { settingKey, o });
			}
		}

		public static void WriteSettings()
		{
			StringBuilder sb = new StringBuilder();
			lock (settingsMutex)
			{
				foreach (KeyValuePair<Type, object> dict in typedSettings)
				{
					Type t = dict.Key;
					System.Collections.IDictionary typedDict = (System.Collections.IDictionary)dict.Value;
					Func<dynamic, string> saveAction = saveActions[t];

					foreach (dynamic setting in typedDict)
					{
						sb.AppendLine(reverseTypePrefixes[t] + ((Setting)setting.Key).ToString() + "=" + saveAction(setting.Value));
					}
				}
			}

			lock (fileMutex)
			{
				using (FileStream fileStream = new FileStream(settingsFileLocation, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter streamWriter = new StreamWriter(fileStream))
						streamWriter.Write(sb.ToString());
				}
			}
		}
	}
}
