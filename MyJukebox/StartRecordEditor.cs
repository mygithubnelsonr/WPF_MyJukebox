using MyJukeboxWMPDapper.DataAccess;
using System;
using System.Diagnostics;

namespace MyJukeboxWMPDapper
{
    class MyProcess : Process
    {
        public void Stop()
        {
            this.CloseMainWindow();
            this.Close();
            OnExited();
        }
    }

    class StartRecordEditor : Process
    {
        public void DefineProcess(string ids)
        {
            MyProcess p = new MyProcess();
            p.StartInfo.FileName = SettingsDb.Settings["RecordEditorLocation"];     // @"C:\Company\Apps\Multimedia\MyRecordEditor\MyRecordEditor.exe";
            p.StartInfo.Arguments = ids;
            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(myProcess_HasExited);
            p.Start();
            p.WaitForInputIdle();
            //p.Stop();
        }

        static void myProcess_HasExited(object sender, System.EventArgs e)
        {
            MainWindow.processEnded = true;
        }
    }
}
