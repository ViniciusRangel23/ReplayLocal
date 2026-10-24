using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Kairos.Engine;
using Kairos.Engine.Audio;
using Kairos.Settings;
using Kairos.Ui;

namespace Kairos.Tests;

/// <summary>Explicitly selected only: captures silence emitted by our own uniquely named helper. No video source or mic.</summary>
public static class NativeApplicationAudioTests
{
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int command);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool SetDllDirectory(string? path);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text,uint revision,out IntPtr descriptor,out uint bytes);
    [DllImport("advapi32.dll",SetLastError=true)]static extern bool SetKernelObjectSecurity(IntPtr handle,uint information,IntPtr descriptor);
    [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr memory);
    static void DenyModuleMemoryRead(Process ownHelper)
    {
        // Change only our disposable child: deny PROCESS_VM_READ, retain query/synchronize/terminate.
        if(!ConvertStringSecurityDescriptorToSecurityDescriptor("D:(D;;0x0010;;;WD)(A;;GA;;;WD)",1,out var descriptor,out _))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            if(!SetKernelObjectSecurity(ownHelper.Handle,4,descriptor))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        finally{LocalFree(descriptor);}
        bool denied=false;
        try{using var reopened=Process.GetProcessById(ownHelper.Id);_=reopened.MainModule?.FileName;}
        catch(System.ComponentModel.Win32Exception){denied=true;}
        Assert.True(denied,"controlled helper really denies the old MainModule query");
    }
    sealed class QuietWindow:System.Windows.Forms.Form
    {
        protected override bool ShowWithoutActivation=>true;
    }
    internal static int FixtureWindow(string title)
    {
        using var window=new QuietWindow{Text=title,ShowInTaskbar=false,StartPosition=System.Windows.Forms.FormStartPosition.Manual,Location=new(-32000,-32000),Size=new(200,100)};
        using var sound=new SoundOut(48000,2,mute:true);
        byte[] silence=new byte[4800*4];sound.Write(silence);sound.Start();
        using var timer=new System.Windows.Forms.Timer{Interval=30};
        timer.Tick+=(_,_)=>{if(sound.WrittenSeconds-sound.PlayedSeconds<0.2)sound.Write(silence);};timer.Start();
        using var expiry=new System.Windows.Forms.Timer{Interval=60000};expiry.Tick+=(_,_)=>window.Close();expiry.Start();
        System.Windows.Forms.Application.Run(window);return 0;
    }
    [Test]
    public static void Native_WASAPI_receives_only_controlled_helper_silence()
    {
        string root=Environment.GetEnvironmentVariable("REPLAYLOCAL_NATIVE_RUNTIME")??throw new InvalidOperationException("Explicit native runtime required; this test never chooses a personal process.");
        Assert.True(File.Exists(Path.Combine(root,"obs.dll")),"packaged OBS runtime");
        var extraFiles=new List<string>();var extraDirectories=new List<string>();
        var helpers=new List<string>();var children=new List<Process>();var candidates=new List<AudioTargetCandidate>();
        string oldDirectory=Environment.CurrentDirectory;
        try
        {
            for(int number=0;number<4;number++)
            {
                string helper=Path.Combine(AppContext.BaseDirectory,"ReplayLocalSyntheticAudio-"+Guid.NewGuid().ToString("N")+".exe");helpers.Add(helper);
                File.Copy(Path.Combine(AppContext.BaseDirectory,"Kairos.Tests.exe"),helper);
                var child=Process.Start(new ProcessStartInfo(helper){UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"--audio-fixture-window","Replay Local Synthetic Silence "+number}})!;children.Add(child);
                if(number==0)DenyModuleMemoryRead(child);
                AudioTargetCandidate? candidate=null;
                for(int i=0;i<50&&candidate==null;i++){Thread.Sleep(100);candidate=ApplicationAudioWindows.List().FirstOrDefault(c=>c.ProcessId==child.Id&&c.Target.ExecutablePath.Equals(helper,StringComparison.OrdinalIgnoreCase));}
                Assert.True(candidate!=null,"our controlled helper is discoverable even when module memory reads are denied");candidates.Add(candidate!);
            }
            SetDllDirectory(root);
            NativeLibrary.SetDllImportResolver(typeof(Kairos.Native.Obs).Assembly,(name,_,_)=>name=="obs"?NativeLibrary.Load(Path.Combine(root,"obs.dll")):IntPtr.Zero);
            string scratch=Path.Combine(Path.GetTempPath(),"replay-native-audio",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
            ObsHost.Start(root,scratch,Path.Combine(scratch,"logs"),new(64,64,64,64,30),VideoCodec.H264);
            Assert.True(ObsHost.ApplicationAudioSupported(),"WASAPI process loopback supported");
            var measurements=new List<object>();
            foreach(int count in new[]{1,4})
            {
                var settings=new AudioSettings{Mode=AudioCaptureMode.Applications,Applications=candidates.Take(count).Select((c,i)=>new ApplicationAudioSettings{Id="synthetic"+i,Name="Synthetic silence "+i,AutomaticGame=count==1,Target=c.Target}).ToList()};
                var backend=new ObsApplicationAudioBackend(settings.Applications.Select(a=>a.Id).ToArray());
                using(var session=new ApplicationAudioSession(settings,backend))
                {
                    session.Refresh(candidates.Take(count).ToArray(),count==1?candidates[0]:null);
                    for(int i=0;i<60&&settings.Applications.Any(a=>backend.MeterUpdatesFor(a.Id)==0);i++)Thread.Sleep(100);
                    Assert.True(settings.Applications.All(a=>backend.MeterUpdatesFor(a.Id)>0),"every controlled source delivered native packets; no desktop/mic/video source");
                    if(count==1)
                    {
                        session.Refresh(candidates,candidates[1]);
                        for(int poll=0;poll<60&&backend.MeterUpdatesFor("synthetic0")==0;poll++)Thread.Sleep(100);
                        Assert.True(backend.MeterUpdatesFor("synthetic0")>0,"automatic game source switches to second controlled helper");
                        Assert.Equal(candidates[1].Target.ExecutableName,session.Status[0].ApplicationName);
                        long packets=backend.MeterUpdatesFor("synthetic0");session.Refresh(candidates);Thread.Sleep(300);
                        Assert.True(backend.MeterUpdatesFor("synthetic0")>packets,"automatic source survives simulated Alt+Tab");
                    }
                    for(int run=0;run<3;run++)
                    {
                        using var self=Process.GetCurrentProcess();var cpu=self.TotalProcessorTime;var watch=Stopwatch.StartNew();Thread.Sleep(1000);watch.Stop();self.Refresh();
                        measurements.Add(new{sources=count,run,elapsedMs=watch.Elapsed.TotalMilliseconds,cpuMs=(self.TotalProcessorTime-cpu).TotalMilliseconds,workingSetMb=self.WorkingSet64/1048576d,peakWorkingSetMb=self.PeakWorkingSet64/1048576d});
                    }
                    if(count==4)
                    {
                        string id=settings.Applications[0].Id;long before=backend.MeterUpdatesFor(id);
                        ShowWindow(children[0].MainWindowHandle,0);
                        var hidden=ApplicationAudioWindows.List(includeHidden:true).Where(c=>candidates.Any(t=>t.ProcessId==c.ProcessId)).ToArray();
                        session.Refresh(hidden);Thread.Sleep(400);
                        Assert.True(backend.MeterUpdatesFor(id)>before,"hiding our selected live helper preserves audio");
                        Assert.Equal("available",session.Status[0].State);
                        string foreignDir=Path.Combine(AppContext.BaseDirectory,"foreign-helper-"+Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(foreignDir);extraDirectories.Add(foreignDir);
                        foreach(string file in new[]{"Kairos.Tests.dll","Kairos.Tests.deps.json","Kairos.Tests.runtimeconfig.json","ReplayLocal.dll"})
                        {string copy=Path.Combine(foreignDir,file);File.Copy(Path.Combine(AppContext.BaseDirectory,file),copy);extraFiles.Add(copy);}
                        string foreignExe=Path.Combine(foreignDir,Path.GetFileName(helpers[0]));File.Copy(helpers[0],foreignExe);extraFiles.Add(foreignExe);
                        children[0].Kill();children[0].WaitForExit(5000);
                        var foreign=Process.Start(new ProcessStartInfo(foreignExe){UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"--audio-fixture-window","Replay Local Synthetic Silence 0"}})!;children.Add(foreign);
                        bool found=false;
                        for(int poll=0;poll<40&&!found;poll++){Thread.Sleep(100);found=ApplicationAudioWindows.List().Any(c=>c.ProcessId==foreign.Id);}
                        Assert.True(found,"controlled same-basename replacement started");
                        // Deliberately DO NOT refresh the session: the audio-thread guard must work by itself.
                        long rejected=backend.RejectedBlocksFor(id);
                        for(int poll=0;poll<90&&backend.RejectedBlocksFor(id)==rejected;poll++)Thread.Sleep(100);
                        Assert.True(backend.RejectedBlocksFor(id)>rejected,"native foreign reconnect reached guard and was rejected");
                        long accepted=backend.MeterUpdatesFor(id);Thread.Sleep(400);
                        Assert.Equal(accepted,backend.MeterUpdatesFor(id),"foreign packets never reach meters/mixers even with owner refresh paused");
                    }
                    session.Refresh([]);Assert.True(session.Status.All(s=>s.State is "unavailable" or "waitingGame"),"all disconnected");
                }
            }
            ObsHost.DrainDestroyQueue();Assert.Equal(0,ObsHost.AliveSources().Count,"controlled sources all released");
            string? evidence=Environment.GetEnvironmentVariable("REPLAYLOCAL_TEST_EVIDENCE");
            if(evidence!=null){Directory.CreateDirectory(evidence);File.WriteAllText(Path.Combine(evidence,"native-performance.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));}
        }
        finally
        {
            if(ObsHost.Started)ObsHost.Shutdown();
            SetDllDirectory(null);
            Environment.CurrentDirectory=oldDirectory;
            foreach(var child in children){if(!child.HasExited){child.Kill();child.WaitForExit(5000);}child.Dispose();}
            foreach(string helper in helpers)File.Delete(helper);
            foreach(string file in extraFiles)File.Delete(file);
            foreach(string directory in extraDirectories)Directory.Delete(directory);
        }
    }
}
