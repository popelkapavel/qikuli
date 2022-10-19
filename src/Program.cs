using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security;

namespace qikuli
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Form1 f=new Form1(args);
      f.Show();
      //Message msg=new Message();
      while(!f.closed) {
        f.Lazy();
        WaitMessage();
        Application.DoEvents();
        //if(!PeekMessage(ref msg,IntPtr.Zero,0,0,PM_NOREMOVE)) {
      }
      //Application.Run(new Form1(args));
    }
    [DllImport("user32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
	  public static extern bool WaitMessage();
    [DllImport("user32.dll"), SuppressUnmanagedCodeSecurity, PreserveSig]
    public static extern bool PeekMessage(ref Message msg,IntPtr hwnd,uint fMin,uint fMax,uint remove);  
    public const int PM_NOREMOVE=0;
    public const int PM_REMOVE=1;
    
  }

}
