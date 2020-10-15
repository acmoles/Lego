using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PersistentStorage : MonoBehaviour
{


	string savePath;

	void Awake()
	{
		savePath = Path.Combine(Application.persistentDataPath, "saveFile");
	}

	public void Save(Persistable o, int version)
	{
		using (
			var writer = new BinaryWriter(File.Open(savePath, FileMode.Create))
		)
		{
			writer.Write(-version);
			o.Save(new DataWriter(writer));
		}
	}

	public void Load(Persistable o)
	{
		using (
			var reader = new BinaryReader(File.Open(savePath, FileMode.Open))
		)
		{
			o.Load(new DataReader(reader, -reader.ReadInt32()));
		}
	}

}
