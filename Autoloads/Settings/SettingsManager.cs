using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsManager : Node
{
	#region Constants

	public const string VIDEO_RESOLUTION = "video/resolution";
	public const string VIDEO_SCREEN_MODE = "video/screen_mode";

	public const string AUDIO_MASTER = "audio/master";
	public const string AUDIO_MUSIC = "audio/music";
	public const string AUDIO_SFX = "audio/sfx";

	public const string GAMEPLAY_LOCALE = "gameplay/locale";

	public static readonly Vector2I[] RESOLUTIONS =
	{
		new Vector2I(1280, 720),
		new Vector2I(1366, 768),
		new Vector2I(1600, 900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440),
		new Vector2I(3840, 2160)
	};

	public static readonly (string Action, Key Default)[] BINDINGS =
	{
		("forward", Key.W),
		("left", Key.A),
		("backward", Key.S),
		("right", Key.D),
		("rotateLeft", Key.Q),
		("rotateRight", Key.E)
	};

	private const string PATH = "user://settings.cfg";

	private const string META_SECTION = "meta";
	private const string META_VERSION = "version";

	private const int VERSION = 1;

	private const float MIN_LEVEL = 0.0001f;

	private const string CONTROLS_SECTION = "controls";

	private const string BUS_MASTER = "Master";
	private const string BUS_MUSIC = "Music";
	private const string BUS_SFX = "SFX";

	#endregion


	#region Events

	public static event Action<string> Changed;

	#endregion


	#region State

	public static SettingsManager Instance { get; private set; }

	private readonly Dictionary<string, SettingDefinition> Definitions = new();
	private readonly Dictionary<string, Variant> Values = new();
	private readonly List<string> Order = new();

	private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> Authored = new();

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Instance = this;

		Capture();
		Define();
		Load();
		ApplyAll();
	}


	public override void _ExitTree()
	{
		Instance = null;
	}

	#endregion


	#region Definitions

	public static string ControlKey(string action)
	{
		return $"controls/{action}";
	}


	private void Capture()
	{
		foreach ((string action, Key _) in BINDINGS)
		{
			if (InputMap.HasAction(action))
			{
				Authored[action] = InputMap.ActionGetEvents(action);
			}
		}
	}


	private void Define()
	{
		Add(VIDEO_RESOLUTION, new Vector2I(1920, 1080), value => ApplyResolution(value.AsVector2I()));
		Add(VIDEO_SCREEN_MODE, 0, value => ApplyScreenMode(value.AsInt32()));

		Add(AUDIO_MASTER, 1f, value => ApplyBus(BUS_MASTER, value.AsSingle()));
		Add(AUDIO_MUSIC, 1f, value => ApplyBus(BUS_MUSIC, value.AsSingle()));
		Add(AUDIO_SFX, 1f, value => ApplyBus(BUS_SFX, value.AsSingle()));

		foreach ((string action, Key key) in BINDINGS)
		{
			string captured = action;

			Add(ControlKey(action), (int)key, value => ApplyBinding(captured, (Key)value.AsInt32()));
		}

		Add(GAMEPLAY_LOCALE, "en", value => TranslationServer.SetLocale(value.AsString()));
	}


	private void Add(string key, Variant fallback, Action<Variant> apply)
	{
		Definitions[key] = new SettingDefinition
		{
			Default = fallback,
			Apply = apply
		};

		Values[key] = fallback;

		Order.Add(key);
	}

	#endregion


	#region Access

	public Variant Get(string key)
	{
		if (Values.TryGetValue(key, out Variant value))
		{
			return value;
		}

		GD.PushWarning($"[Settings] unknown key \"{key}\"");

		return default;
	}


	public Variant GetDefault(string key)
	{
		return Definitions.TryGetValue(key, out SettingDefinition definition) ? definition.Default : default;
	}


	public string FindBinding(Key key, string except)
	{
		foreach ((string action, Key _) in BINDINGS)
		{
			if (action == except)
			{
				continue;
			}

			if (Get(ControlKey(action)).AsInt32() == (int)key)
			{
				return action;
			}
		}

		return null;
	}


	public bool Set(string key, Variant value)
	{
		if (!Definitions.TryGetValue(key, out SettingDefinition definition))
		{
			GD.PushWarning($"[Settings] unknown key \"{key}\"");
			return false;
		}

		if (value.VariantType != definition.Default.VariantType)
		{
			GD.PushWarning($"[Settings] \"{key}\" expects {definition.Default.VariantType}, got {value.VariantType}");
			return false;
		}

		if (SectionOf(key) == CONTROLS_SECTION && FindBinding((Key)value.AsInt32(), NameOf(key)) != null)
		{
			GD.PushWarning($"[Settings] \"{key}\" would duplicate an existing binding");
			return false;
		}

		Values[key] = value;

		definition.Apply?.Invoke(value);

		Changed?.Invoke(key);

		return true;
	}


	public void Reset(string key)
	{
		if (Definitions.TryGetValue(key, out SettingDefinition definition))
		{
			Set(key, definition.Default);
		}
	}


	public void ResetSection(string section)
	{
		if (section == CONTROLS_SECTION)
		{
			foreach (string key in Keys(section))
			{
				Values[key] = Definitions[key].Default;
			}

			foreach (string key in Keys(section))
			{
				Definitions[key].Apply?.Invoke(Values[key]);

				Changed?.Invoke(key);
			}

			return;
		}

		foreach (string key in Keys(section))
		{
			Reset(key);
		}
	}


	public IEnumerable<string> Keys(string section)
	{
		string prefix = $"{section}/";

		foreach (string key in Order)
		{
			if (key.StartsWith(prefix))
			{
				yield return key;
			}
		}
	}

	#endregion


	#region Storage

	public void Reload()
	{
		Load();
		ApplyAll();
	}


	public void Load()
	{
		ConfigFile file = new ConfigFile();

		Error result = file.Load(PATH);

		if (result != Error.Ok)
		{
			GD.Print($"[Settings] no usable file at {PATH} ({result}), keeping defaults");

			foreach (string key in Order)
			{
				Values[key] = Definitions[key].Default;
			}

			return;
		}

		int version = file.GetValue(META_SECTION, META_VERSION, 0).AsInt32();

		if (version != VERSION)
		{
			GD.PushWarning($"[Settings] file version {version}, expected {VERSION}");
		}

		foreach (string key in Order)
		{
			SettingDefinition definition = Definitions[key];

			string section = SectionOf(key);
			string name = NameOf(key);

			if (!file.HasSection(section) || !file.HasSectionKey(section, name))
			{
				Values[key] = definition.Default;
				continue;
			}

			Variant stored = file.GetValue(section, name);

			if (stored.VariantType != definition.Default.VariantType)
			{
				GD.PushWarning($"[Settings] \"{key}\" has the wrong type in the file, keeping the default");

				Values[key] = definition.Default;
				continue;
			}

			Values[key] = stored;
		}

		GD.Print($"[Settings] loaded {Order.Count} values from {PATH}");
	}


	public void Save()
	{
		ConfigFile file = new ConfigFile();

		file.SetValue(META_SECTION, META_VERSION, VERSION);

		foreach (string key in Order)
		{
			file.SetValue(SectionOf(key), NameOf(key), Values[key]);
		}

		Error result = file.Save(PATH);

		if (result != Error.Ok)
		{
			GD.PushWarning($"[Settings] failed to save {PATH}: {result}");
		}
	}


	private static string SectionOf(string key)
	{
		int slash = key.IndexOf('/');

		return slash < 0 ? "general" : key[..slash];
	}


	private static string NameOf(string key)
	{
		int slash = key.IndexOf('/');

		return slash < 0 ? key : key[(slash + 1)..];
	}

	#endregion


	#region Applying

	private void ApplyAll()
	{
		foreach (string key in Order)
		{
			Definitions[key].Apply?.Invoke(Values[key]);
		}
	}


	private static void ApplyResolution(Vector2I size)
	{
		if (size.X <= 0 || size.Y <= 0)
		{
			return;
		}

		DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();

		if (mode == DisplayServer.WindowMode.Fullscreen || mode == DisplayServer.WindowMode.ExclusiveFullscreen)
		{
			return;
		}

		if (mode == DisplayServer.WindowMode.Maximized)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}

		DisplayServer.WindowSetSize(size);

		Vector2I screen = DisplayServer.ScreenGetSize();

		DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition() + (screen - size) / 2);
	}


	private static void ApplyScreenMode(int mode)
	{
		DisplayServer.WindowMode target = mode switch
		{
			1 => DisplayServer.WindowMode.Fullscreen,
			2 => DisplayServer.WindowMode.ExclusiveFullscreen,
			_ => DisplayServer.WindowMode.Windowed
		};

		DisplayServer.WindowSetMode(target);
	}


	private static void ApplyBus(string bus, float level)
	{
		int index = AudioServer.GetBusIndex(bus);

		if (index < 0)
		{
			GD.PushWarning($"[Settings] audio bus \"{bus}\" does not exist");
			return;
		}

		AudioServer.SetBusMute(index, level <= MIN_LEVEL);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Clamp(level, MIN_LEVEL, 1f)));
	}


	private void ApplyBinding(string action, Key key)
	{
		if (!InputMap.HasAction(action))
		{
			GD.PushWarning($"[Settings] input action \"{action}\" does not exist");
			return;
		}

		InputMap.ActionEraseEvents(action);

		if (IsAuthored(action, key))
		{
			foreach (InputEvent original in Authored[action])
			{
				InputMap.ActionAddEvent(action, original);
			}

			return;
		}

		InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
	}


	private bool IsAuthored(string action, Key key)
	{
		if (!Authored.ContainsKey(action))
		{
			return false;
		}

		return Definitions[ControlKey(action)].Default.AsInt32() == (int)key;
	}

	#endregion
}
