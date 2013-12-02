using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;

public static class OuyaBuildUtility {

	[MenuItem ("OUYA/Input/Export OuyaInput Core")]
	internal static void ExportCore () {
		string[] assets = new string[] {
			"ProjectSettings/InputManager.asset",
			"Assets/Plugins/OuyaInput/OuyaInput.cs"
		};

		Export (assets);
	}

	[MenuItem ("OUYA/Input/Export OuyaInput Full")]
	internal static void ExportFull () {
		string[] assets = new string[] {
			"ProjectSettings/InputManager.asset",
			"Assets/Plugins/OuyaInput/OuyaInput.cs",
			"Assets/OuyaInput/Scripts/InputHandlerPattern.cs",
			"Assets/OuyaInput/Scripts/InputHandlerPattern.js",
			"Assets/OuyaInput/Scripts/OuyaInputTester.cs",
			"Assets/OuyaInput/Docs/ControllerDocumentation.pdf"
		};

		Export (assets);
	}

	static void Export (params string[] assets) {
		assets.Where (asset => !File.Exists (asset) && !Directory.Exists (asset)).ToList ().ForEach (asset => Debug.LogWarning ("Couldn't find asset " + asset + ". Won't be included in the package. "));
		string path = EditorUtility.SaveFilePanel ("Export OuyaInput", "Assets/", "OuyaInput.unitypackage", "unitypackage");
		if (path.Length > 0) {
			AssetDatabase.ExportPackage (assets.Where (asset => File.Exists (asset) || Directory.Exists (asset)).ToArray (), path);
			AssetDatabase.Refresh ();
		}
	}
}