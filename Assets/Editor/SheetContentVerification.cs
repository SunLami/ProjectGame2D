#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Explicit fallback for environments where the Unity Test Runner cannot initialize PlayMode.
// SessionState survives domain reload, but no check runs without the explicit Start command.
[InitializeOnLoad]
public static class SheetContentVerification
{
    private const string Pending="ProjectGame2D.SheetContent.PlayChecks";
    [Serializable] private class Check { public string name; public bool passed; public string error; }
    [Serializable] private class Report { public string method="Live PlayMode reflection smoke"; public List<Check> checks=new(); }
    static SheetContentVerification()
    {
        EditorApplication.playModeStateChanged += OnState;
        if(SessionState.GetBool(Pending,false) && EditorApplication.isPlaying)
            EditorApplication.delayCall += Run;
    }
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before starting checks.");
        if(EditorUtility.scriptCompilationFailed)throw new InvalidOperationException("Compilation errors remain.");
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    private static void OnState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Run;
    }
    private static void Run()
    {
        if(!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying)return;
        SessionState.SetBool(Pending,false);
        var report=new Report();
        Type fixtureType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SheetContentPlayModeTests")).FirstOrDefault(t=>t!=null);
        if(fixtureType==null)report.checks.Add(new Check{name="Load test assembly",passed=false,error="SheetContentPlayModeTests was not loaded."});
        else foreach(var method in fixtureType.GetMethods(BindingFlags.Instance|BindingFlags.Public))
        {
            var cases=method.GetCustomAttributesData().Where(a=>a.AttributeType.Name=="TestCaseAttribute").ToArray();
            bool simple=method.GetCustomAttributesData().Any(a=>a.AttributeType.Name=="TestAttribute");
            if(!simple && cases.Length==0)continue;
            IEnumerable<object[]> arguments=simple?new[]{Array.Empty<object>()}:cases.Select(c=>c.ConstructorArguments.Select(a=>a.Value).ToArray());
            foreach(var args in arguments)
            {
                object fixture=Activator.CreateInstance(fixtureType);bool setup=false;
                var check=new Check{name=method.Name+"("+string.Join(",",args)+")"};
                try
                {
                    fixtureType.GetMethod("Setup").Invoke(fixture,null);setup=true;
                    method.Invoke(fixture,args);check.passed=true;
                }
                catch(Exception e){check.error=(e.InnerException??e).ToString();}
                finally
                {
                    if(setup)try{fixtureType.GetMethod("Teardown").Invoke(fixture,null);}
                    catch(Exception e){check.passed=false;check.error+="\nTeardown: "+(e.InnerException??e);}
                }
                report.checks.Add(check);
            }
        }
        Directory.CreateDirectory("Temp/SheetContentVerification");
        File.WriteAllText("Temp/SheetContentVerification/live-playmode.json",JsonUtility.ToJson(report,true));
        Debug.Log("Sheet content live PlayMode checks: "+report.checks.Count(c=>c.passed)+"/"+report.checks.Count+" passed. Personal save files untouched.");
        EditorApplication.ExitPlaymode();
    }
}
#endif
