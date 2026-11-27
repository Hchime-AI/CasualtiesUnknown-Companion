using System.IO;

namespace CUCompanion;

public class PersonaStore
{
	public int version;

	public Persona[] slots = new Persona[3];

	public static PersonaStore CreateDefault()
	{
		PersonaStore personaStore = new PersonaStore
		{
			version = 5
		};
		for (int i = 0; i < 3; i++)
		{
			LocalizationManager.DefaultPersona(i, out var callPlayer, out var interactions, out var personality, out var appearance, out var relationship, out var traits);
			personaStore.slots[i] = new Persona
			{
				gender = i switch
				{
					2 => 2, 
					1 => 1, 
					_ => 0, 
				},
				name = i switch
				{
					2 => "Dune", 
					1 => "Milky", 
					_ => "Experiment", 
				},
				callPlayer = callPlayer,
				interactions = interactions,
				personality = personality,
				appearance = appearance,
				relationship = relationship,
				traits = traits,
				bodyTint = false,
				bodyHue = i switch
				{
					2 => 0.65f, 
					1 => 0.35f, 
					_ => 0.08f, 
				},
				bodyValue = i switch
				{
					2 => 0.95f, 
					1 => 0.75f, 
					_ => 0.85f, 
				},
				eyeTint = false,
				eyeHue = 0.55f,
				eyeValue = 0.9f
			};
		}
		return personaStore;
	}

	public Persona Get(int charType)
	{
		if (slots == null || slots.Length == 0)
		{
			return new Persona();
		}
		int num = charType;
		if (num < 0 || num >= slots.Length)
		{
			num = 0;
		}
		return slots[num] ?? (slots[num] = new Persona());
	}

	public static PersonaStore Load()
	{
		return LoadOrCreate();
	}

	public static PersonaStore LoadOrCreate()
	{
		string personaPath = StorageManager.PersonaPath;
		PersonaStore personaStore = null;
		if (File.Exists(personaPath))
		{
			personaStore = StorageManager.Load<PersonaStore>(personaPath);
		}
		if (personaStore == null || personaStore.slots == null || personaStore.slots.Length != 3)
		{
			personaStore = CreateDefault();
			StorageManager.Save(personaPath, personaStore);
		}
		return personaStore;
	}

	public void Save()
	{
		StorageManager.Save(StorageManager.PersonaPath, this);
	}
}
