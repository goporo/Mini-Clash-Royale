// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.70
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

public partial class UnitSpawnedMessage : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public UnitSpawnedMessage() { }
	[Type(0, "string")]
	public string unitId = default(string);

	[Type(1, "float32")]
	public float x = default(float);

	[Type(2, "float32")]
	public float y = default(float);
}

