using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Demo02.Editor
{
    internal static class Demo02DataRegressionSuite
    {
        public static void VerifyContentCatalog()
        {
            Demo02ContentPipeline.ValidateContent();
        }

    }
}
