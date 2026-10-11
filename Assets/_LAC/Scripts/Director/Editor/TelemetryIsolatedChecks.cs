#if LAC_TELEMETRY_ISOLATED
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;
using LAC.Core;
using LAC.Director;
using LAC.Player;
using UnityEngine;

class Program
{
    static int checks;
    static void Check(bool c,string msg) { if(!c) throw new Exception(msg); checks++; }
    static List<Dictionary<string,string>> Read(string path) {
        using var p=new TextFieldParser(path); p.SetDelimiters(","); p.HasFieldsEnclosedInQuotes=true;
        string[] head=p.ReadFields(); var rows=new List<Dictionary<string,string>>();
        while(!p.EndOfData) {var data=p.ReadFields(); Check(data.Length==head.Length,"Columns"); rows.Add(head.Zip(data,(k,v)=>(k,v)).ToDictionary(x=>x.k,x=>x.v));} return rows;
    }
    static List<Dictionary<string,string>> Rows(TelemetrySession s,string file)=>Read(Path.Combine(s.DirectoryPath,file+".csv"));
    static void Main() {
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
        Application.persistentDataPath=Path.Combine(Path.GetTempPath(),"lac-t43-harness-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Application.persistentDataPath);
        string csv=Path.Combine(Application.persistentDataPath,"roundtrip.csv");
        using(var w=new TelemetryCsv(csv,"text","number")) {w.Row("Hồn, \"gió\"\r\nDòng 2",1.25);}
        var rt=Read(csv)[0]; Check(rt["text"]=="Hồn, \"gió\"\r\nDòng 2","Quoted multiline UTF8"); Check(rt["number"]=="1.25","Locale leaked");
        Check(File.ReadAllBytes(csv).Take(3).SequenceEqual(new byte[]{239,187,191}),"BOM");
        var run=new RunManager(); var a=new PlayerCharacter(); PlayerRegistry.All.Add(a);
        Mirror.NetworkServer.active=false; Check(TelemetrySession.Begin(run)==null,"Client opened session"); Mirror.NetworkServer.active=true;
        var s=TelemetrySession.Begin(run); s.Join(a); s.StartWave(1); s.StartWave(1);
        s.RecordWaveSpec(new WaveSpec(),"Bảng, \"đợt\"");
        Time.unscaledTimeAsDouble=1.25; a.Health.Health=5; s.Damage(a,1); s.Dash(a); s.Damage(a,0);
        GameEvents.Kill(17); run.SoulsCollected=3; run.DrumActivations=1;
        Time.unscaledTimeAsDouble=2; s.EndWave("cleared"); s.EndWave("cleared");
        var w1=Rows(s,"waves"); Check(w1.Count==1,"Duplicate wave"); Check(w1[0]["health_lost"]=="1" && w1[0]["hits_taken"]=="1","Damage mismatch");
        Check(w1[0]["team_kills"]=="1" && w1[0]["team_souls"]=="3" && w1[0]["team_drum_uses"]=="1","Team counters");
        Check(w1[0]["duration_s"]=="2","Duration locale");
        Time.unscaledTimeAsDouble=10; s.StartWave(2); Time.unscaledTimeAsDouble=11; a.Health.Health=0; s.Damage(a,5); s.Finish("defeat"); s.Finish("aborted");
        var totals=Rows(s,"runs"); Check(totals.Count==1,"Duplicate run"); Check(totals[0]["health_lost"]=="6" && totals[0]["downs"]=="1","Lethal damage");
        Check(totals[0]["dashes"]=="1","Dash total"); Check(Rows(s,"waves")[1]["duration_s"]=="1","Card selection counted in wave");
        var events=Rows(s,"events"); Check(events.Count(x=>x["event"]=="run_ended")==1,"Run end events");
        Check(events.Count(x=>x["event"]=="player_joined")==1,"Double join");
        Check(events.FindIndex(x=>x["event"]=="player_downed")<events.FindIndex(x=>x["event"]=="run_ended"),"Lethal order");
        int index=0; foreach(var e in events) Check(e["seq"]==(++index).ToString(),"Sequence");
        a.Health.Health=6; run.SoulsCollected=0; run.DrumActivations=0;
        var coop=TelemetrySession.Begin(run); Check(coop.DirectoryPath!=s.DirectoryPath,"Same seed overwrote session"); coop.StartWave(1);
        Time.unscaledTimeAsDouble=12; var b=new PlayerCharacter {CharacterId=CharacterId.Archer}; coop.Join(b);
        b.Health.Health=0; coop.Damage(b,6); Time.unscaledTimeAsDouble=15; b.Health.Health=3; coop.Revive(b);
        b.Health.Health=0; coop.Damage(b,3); Time.unscaledTimeAsDouble=16; coop.Leave(b); coop.Leave(b);
        b.Health.Health=999; Time.unscaledTimeAsDouble=18; coop.EndWave("cleared"); coop.StartWave(2); Time.unscaledTimeAsDouble=19; coop.Finish("aborted");
        var cr=Rows(coop,"runs"); Check(cr.Count==2,"Coop roster lost"); Check(cr[1]["health_lost"]=="9" && cr[1]["revives"]=="1" && cr[1]["downs"]=="2","Coop counters");
        Check(cr[1]["health_end"]=="0" && cr[1]["connected"]=="0" && cr[1]["exposure_s"]=="4","Disconnected snapshot/exposure");
        Check(cr[0]["run_players_start"]=="1" && cr[0]["run_players_peak"]=="2" && cr[0]["players_end"]=="1","Roster sizes");
        Check(Rows(coop,"waves").Count==3,"Disconnected player included in future wave");
        Check(Rows(coop,"events").Count(x=>x["event"]=="player_left")==1,"Duplicate leave");
        Check(cr[0]["health_lost"]=="0","Run counters leaked");
        var win=TelemetrySession.Begin(run); for(int wave=1;wave<=16;wave++) {win.StartWave(wave); Time.unscaledTimeAsDouble++; win.EndWave("cleared");} win.Finish("victory");
        Check(Rows(win,"waves").Count==16,"Final wave missing"); Check(Rows(win,"runs")[0]["waves_cleared"]=="16","Clear count");
        int warns=Debug.Warnings;
        string blocker=Path.Combine(Application.persistentDataPath,"file-not-directory"); File.WriteAllText(blocker,"x");
        var fail=(TelemetrySession)Activator.CreateInstance(typeof(TelemetrySession),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{run,blocker},null);
        fail.StartWave(1); fail.Tick(); fail.Finish("aborted"); Check(!fail.IsRecording && Debug.Warnings==warns+1,"I/O failure not isolated");
        var invalid=(TelemetrySession)Activator.CreateInstance(typeof(TelemetrySession),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{run,null},null);
        invalid.StartWave(1); invalid.Tick(); invalid.Finish("aborted");
        Check(!invalid.IsRecording && Debug.Warnings==warns+2,"Invalid path escaped telemetry failure boundary");
        // Five-second flush leaves the file readable before graceful shutdown.
        var periodic=TelemetrySession.Begin(run); periodic.StartWave(1); Time.unscaledTimeAsDouble+=6; periodic.Dash(a); periodic.Tick();
        Check(Rows(periodic,"events").Any(x=>x["event"]=="player_dashed"),"Periodic flush missing"); periodic.Finish("aborted");
        Console.WriteLine($"PASS: {checks} assertions (CSV + session test doubles; NOT Unity Play Mode).");
        Console.WriteLine("Artifacts: "+Application.persistentDataPath);
    }
}

// Test doubles only: this harness does not validate Unity/Mirror lifecycle.
namespace UnityEngine {
 public static class Time { public static double unscaledTimeAsDouble; }
 public static class Application { public static string persistentDataPath=""; public static string version="harness"; public static bool isEditor=true; }
 public static class Debug { public static bool isDebugBuild=true; public static int Warnings; public static void Log(string s) {} public static void LogWarning(string s) { Warnings++; Console.WriteLine(s); } }
}
namespace Mirror { public static class NetworkServer { public static bool active=true; } }
namespace LAC.Player {
 public enum CharacterId { Lancer, Warrior, Archer }
 public class PlayerHealth { public int Health=6, MaxHealth=6; }
 public class PlayerCharacter { public CharacterId CharacterId; public PlayerHealth Health=new PlayerHealth(); public T GetComponent<T>() where T:class => Health as T; }
 public static class PlayerRegistry { public static List<PlayerCharacter> All=new List<PlayerCharacter>(); }
}
namespace LAC.Enemies { public class EnemyData { public string name="CoHon"; } public class Enemy { public int Id; public EnemyData Data=new EnemyData(); } }
namespace LAC.Core {
 public class RunManager { public int Seed=431, SoulsCollected; public uint DrumActivations; }
 public static class GameEvents { public static event Action<LAC.Enemies.Enemy> EnemyDied; public static void Kill(int id) => EnemyDied?.Invoke(new LAC.Enemies.Enemy { Id=id }); }
 public class WaveSpec { public class Group { public LAC.Enemies.EnemyData Enemy; public int Count; } public Group[] Mix=Array.Empty<Group>(); public float[] Directions=new[]{.25f}; public float SpawnInterval=.5f; public int Burst=2; public float Duration=0; public int TotalCount=4; }
}

#endif
