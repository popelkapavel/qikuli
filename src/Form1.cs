using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
//using System.Media;

namespace qikuli
{
  public enum UndoMode { Normal,Up,Shift}
  public struct UndoItem {
    public UndoMode mode;
    public int x,y,nx,ny,shift,emptys,moves,full;
    public UndoItem Mirror(int lvl_w) {
      return new UndoItem() {mode=mode,x=lvl_w-1-x,y=y,nx=lvl_w-1-nx,ny=ny,shift=-shift,emptys=emptys,moves=moves,full=full};
    }
  }
  public partial class Form1 : Form {
    static int anim_max=6,timer_interval=20;
    static string imagepath=@".";
    static Image img_wall=Image.FromFile(Path.Combine(imagepath,"wall.png"));
    static Image img_solid=Image.FromFile(Path.Combine(imagepath,"solid.png"));
    static Image img_empty=Image.FromFile(Path.Combine(imagepath,"empty.png"));
    static Image img_me=Image.FromFile(Path.Combine(imagepath,"me.png"));
    static Image img_block=Image.FromFile(Path.Combine(imagepath,"block.png"));
//    static SoundPlayer move_sound,full_sound;
    static bool jump_1,jump_2,jump_3;
    static bool go_up,go_down,go_left,go_right;
    static bool go_up1,go_down1,go_left1,go_right1;
    static bool go_up0,go_down0,go_left0,go_right0;
    static int ww=16,hh=16,scale=1,sx=0,sy=0;
    static int my_x=0,my_y=0,my_x2=0,my_y2=0;
    static bool mous,cursor=true,nomove=false;
    static int mous_x=-1,mous_y=-1,mous_xp=0;        
    static int shift=0;
    static int tick=0;
    static bool dirtydraw=false;
    static int lvl_w,lvl_h,lvl=0;
    static char[] board;
    static int full=0,moves,emptys;
    static string lev_file,rec_file;
    static DateTime lev_filetime,rec_filetime;
    static List<string> level;
    static int undotop=0,undolvl=0,undos=0,redos=0;
    static List<UndoItem> undo=new List<UndoItem>();
    static SortedDictionary<int,int> records=new SortedDictionary<int,int>();
    static bool start,stop;
    static string help;    
    Graphics gr2;
    Bitmap bm2;
    string kb_lvl="";
    int anim=0;
    bool mirror_x;
    string final_text;
    Brush final_brush;
    internal bool closed;
    
    
    Image CharImage(char ch) {
      if(Level.IsWall(ch)) return img_wall;
      if(Level.IsSolid(ch)) return img_solid;
      if(Level.IsBlock(ch)) return img_block;
      return img_empty;
    }
    char BoardXY(int x,int y) {
      x=(x+lvl_w)%lvl_w;
      y=(y+lvl_h)%lvl_h;
      return board[y*lvl_w+x];
    }
    void go_clear() {
      go_up=go_down=go_left=go_right=false;
    }
    void Status(int level,int moves,int emptys) {
      int rec;
      bool isrec=records.TryGetValue(level+1,out rec);
      Text="qikuli - level "+(level+1)+(emptys<1?"":"  moves:"+emptys)+(isrec?" record:"+rec:"")+(moves<=emptys?"":" points:"+(moves-emptys));
    }
    
    void LoadLevel(int l) {
      undotop=0;
      bool ureset=undolvl!=l;

      if(lev_filetime!=File.GetLastWriteTimeUtc(lev_file)) {
        ureset=true;
        level=Level.Load(lev_file,out lev_filetime);
        if(l>=level.Count) l=lvl=level.Count-1;
      }
      if(ureset) {
        undolvl=l;
        undo.Clear();
      }    
      final_text=null;
      full=Level.Init(level[l],ref lvl_w,ref lvl_h,ref board
        ,ref my_x,ref my_y);
      if(mirror_x) {
        for(int x=0,x2=lvl_w-1;x<x2;x++,x2--) {
          for(int y=0;y<lvl_h;y++) {
            char ch=board[y*lvl_w+x];
            board[y*lvl_w+x]=board[y*lvl_w+x2];
            board[y*lvl_w+x2]=ch;
          }
        }
        my_x=lvl_w-1-my_x;
      }
      my_x2=my_x;my_y2=my_y;
      emptys=moves=0;
      start=!(stop=false);
      go_clear();
      Status(l,moves,emptys);
      Graphics gr=this.CreateGraphics();      
      bm2=new Bitmap(lvl_w*img_wall.Width,lvl_h*img_wall.Height,gr);//System.Drawing.Imaging.PixelFormat.Format32bppRgb);      
      if(gr2!=null) gr2.Dispose();
      gr2=Graphics.FromImage(bm2);
      gr.Dispose();
      UpdateSize();
      //DrawBack();
    }
    void Mirror() {
      int x,x2,y,y2;
      char r;
      mirror_x^=true;
      for(y=0;y<lvl_h;y++)
        for(x=0,x2=lvl_w-1;x<x2;x++,x2--) {
          r=board[y*lvl_w+x];
          board[y*lvl_w+x]=board[y*lvl_w+x2];
          board[y*lvl_w+x2]=r;
        }
      my_x2=my_x=lvl_w-my_x-1;
      anim=0;
      for(int i=0;i<undo.Count;i++) undo[i]=undo[i].Mirror(lvl_w);
      DrawBack();
    }    
    static void LoadRecords(string filename) {
      TextReader tr=null;      
     try {
      DateTime filetime=File.GetLastWriteTimeUtc(filename);
      if(filetime==rec_filetime) return;
      rec_filetime=filetime;
      tr=new StreamReader(filename,System.Text.Encoding.Default);
      string line;      
      while(null!=(line=tr.ReadLine())) {
        string[] sa=line.Split(' ','\t');
        int lvl=-1,sai;
        foreach(string sax in sa) {
          if(!int.TryParse(sax,out sai)) continue;
          if(lvl<1) lvl=sai;
          else if(sai>=0) {
            int act;
            if(!records.TryGetValue(lvl,out act)||act<1||act>sai)
              records[lvl]=sai;
            break;
          }
        }
      }
     } catch {
     } finally {
      if(tr!=null) tr.Close(); 
     }
    }
    static void SaveRecords(string filename) {
      TextWriter tw=null;
     try {
      tw=new StreamWriter(filename,false);
      foreach(KeyValuePair<int,int> kv in records)
        tw.WriteLine(""+kv.Key+" "+kv.Value);
     } catch {
     } finally {
      if(tw!=null) {
        tw.Close();
        rec_filetime=File.GetLastWriteTimeUtc(filename);
      }
     }
    }
    void UndoPush(UndoMode mode,int x,int y,int nx,int ny,int shift,int moves,int emptys,int full) {
      if(undotop==0) undolvl=lvl;
      if(undotop<undo.Count) undo.RemoveRange(undotop,undo.Count-undotop);
      undo.Add(new UndoItem() {mode=mode,x=x,y=y,nx=nx,ny=ny,shift=shift,moves=moves,emptys=emptys,full=full});
      undotop=undo.Count;
    }
    void Shift(int x,int y,int shift) {
      if(shift==0) return;
      int dx=shift<0?1:-1;
      int idx=y*lvl_w+x;
      while(shift!=0) {
        board[idx+shift-dx]=board[idx+shift];
        shift+=dx;
      }
      board[idx-dx]=Level.Empty;
    }
    void UpdateXY(Graphics gr,int x,int y) {
      char ch=BoardXY(x,y);
      int sx=x*img_wall.Width,sy=y*img_wall.Height;
      DrawXY(gr2,img_empty,sx,sy);
      if(!Level.IsEmpty(ch)) DrawXY(gr2,CharImage(ch),sx,sy);
      if(gr!=null) CopyBlock(gr,x,y,1,1);
    }
    void Undo() {
      if(undotop<1) { undos=0;return;}
      bool dirty=false;
      Graphics gr=CreateGraphics();
      while(undos>0&&undotop>=1) {
       dirty=true;undos--;       
      undotop--;
      UndoItem ui=undo[undotop];
      UpdateXY(gr,my_x,my_y);
      my_x2=my_x=ui.x;my_y2=my_y=ui.y;emptys=ui.emptys;moves=ui.moves;full=ui.full;
      switch(ui.mode) {
       case UndoMode.Shift:
        if(ui.shift<0) {
          Shift(my_x+ui.shift-2,my_y,-ui.shift);
          for(int i=0;i>=ui.shift;i--) UpdateXY(null,my_x-1+i,my_y);
          CopyBlock(gr,my_x+ui.shift-1,my_y,1-ui.shift,1);
        } else {
          Shift(my_x+ui.shift+2,my_y,-ui.shift);
          for(int i=0;i<=ui.shift;i++) UpdateXY(null,my_x+1+i,my_y);
          CopyBlock(gr,my_x+1,my_y,ui.shift+1,1);
        }        
        break;
       case UndoMode.Up:        
        board[my_x+(my_y-1)*lvl_w]=board[my_x+(my_y)*lvl_w];
        board[my_x+(my_y)*lvl_w]=Level.Empty;
        UpdateXY(gr,my_x,my_y-1);
        break;
       default:board[my_x+(my_y+1)*lvl_w]=Level.Block;
        UpdateXY(gr,my_x,my_y+1);
        break;
      }             
      }
      DrawXY(gr2,img_empty,my_x*img_wall.Width,my_y*img_wall.Height);
      DrawXY(gr2,img_me,my_x*img_wall.Width,my_y*img_wall.Height);
      CopyBlock(gr,my_x,my_y,1,1);  
      gr.Dispose();
      undos=0;
      if(!dirty) return;
      anim=0;
      if(!string.IsNullOrEmpty(final_text)) {
        final_text=null;
        DrawBack();
      }
      stop=false;
      //DrawBack();
      Status(lvl,moves,emptys);
    }
    void Redo() {
      if(undotop>=undo.Count) { redos=0;return;}
      bool dirty=false;
      Graphics gr=CreateGraphics();
      while(redos>0&&undotop<undo.Count) {
       dirty=true;redos--;             
      UndoItem ui=undo[undotop];
      undotop++;
      UpdateXY(gr,my_x,my_y);      
      my_x2=my_x=ui.nx;my_y2=my_y=ui.ny;emptys=ui.emptys;moves=ui.moves;full=ui.full;
      switch(ui.mode) {
       case UndoMode.Shift:
        Shift(my_x,my_y,ui.shift);
        if(ui.shift<0) {
          for(int i=0;i>=ui.shift;i--) UpdateXY(null,my_x-1+i,my_y);
          CopyBlock(gr,my_x+ui.shift-1,my_y,1-ui.shift,1);
        } else {
          for(int i=0;i<=ui.shift;i++) UpdateXY(null,my_x+1+i,my_y);
          CopyBlock(gr,my_x+1,my_y,ui.shift+1,1);
        }                
        break;
       case UndoMode.Up:
        board[ui.x+(ui.y)*lvl_w]=board[ui.x+(ui.y-1)*lvl_w];
        board[ui.x+(ui.y-1)*lvl_w]=Level.Empty;
        moves++;emptys++;
        UpdateXY(gr,ui.x,ui.y);
        break;
       default:board[ui.x+(ui.y+1)*lvl_w]=Level.Empty;moves++;full--;
        UpdateXY(gr,ui.x,ui.y+1);
        break;
      }
      }
      anim=0;
      DrawXY(gr2,img_empty,my_x*img_wall.Width,my_y*img_wall.Height);
      DrawXY(gr2,img_me,my_x*img_wall.Width,my_y*img_wall.Height);
      CopyBlock(gr,my_x,my_y,1,1);  
      gr.Dispose();
      undos=0;
      if(!dirty) return;            
      if(full==0) {
        SetFinal();
        DrawBack();
        stop=true;
      }            
      //DrawBack();
      Status(lvl,moves,emptys);
    }
    public Form1(string[] args) {
      lev_file="levels.txt";
      if(args.Length>0) lev_file=args[0];      
      string ext=Path.GetExtension(lev_file);
      rec_file=string.IsNullOrEmpty(ext)?lev_file+"_rec":lev_file.Substring(0,lev_file.Length-ext.Length)+"_rec"+ext;
      level=Level.Load(lev_file,out lev_filetime);
      LoadRecords(rec_file);
      LoadLevel(lvl);
      InitializeComponent();
      timer1.Interval=timer_interval;
      if(ww*lvl_w>0) {
        this.ClientSize=new Size(ww*lvl_w,hh*lvl_h);
      }
    }

    protected override void OnShown(EventArgs e) {
      base.OnShown(e);
      UpdateSize();
    }
    protected override void OnClosed(EventArgs e) {
      base.OnClosed(e);
      closed=true;
    }    
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
      //Text=""+keyData;
      if(keyData>=Keys.NumPad0&&keyData<=Keys.NumPad9)
        keyData-=Keys.NumPad0-Keys.D0;
      if(keyData>=Keys.D0&&keyData<=Keys.D9)
        kb_lvl+=(char)keyData;
      if(kb_lvl.Length==2||kb_lvl.Length>0&&keyData==Keys.Enter) {
        lvl=int.Parse(kb_lvl)-1;
        kb_lvl="";
        if(lvl<0) lvl=0;
        if(lvl>level.Count-1) lvl=level.Count-1;
        LoadLevel(lvl);
        return true;
      }
      switch(keyData) {
       case Keys.Escape:
         if(start||stop) Close();
         else LoadLevel(lvl);
         return true;
       case Keys.Subtract:
       case Keys.PageDown:
       case Keys.Back:
        if(start&&lvl>0) lvl--;       
        LoadLevel(lvl);return true;
       case Keys.Add:
       case Keys.Enter:
       case Keys.PageUp:
         if(start||stop) if(lvl<level.Count-1) lvl++;;
         LoadLevel(lvl);
         return true;
       case Keys.Delete:
       case Keys.Z:
       case Keys.Z|Keys.Control:
        undos++;
        return true;
       case Keys.Insert:
       case Keys.Y:       
       case Keys.Y|Keys.Control:
        redos++;
        return true;
       case Keys.F1: {
         if(help==null) 
          try { help=File.ReadAllText(Path.Combine(imagepath,"readme.txt"),Encoding.UTF8);
          } catch {
           MessageBox.Show("Unable to read help file 'readme.txt'","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
           break;
          }
         MessageBox.Show(help,"Kulili help",MessageBoxButtons.OK,MessageBoxIcon.Information);
        } break;
       case Keys.X:        
       case Keys.F4:Mirror();break;
       case Keys.Enter|Keys.Shift:
        if(0!=(WindowState&FormWindowState.Maximized)) ChangeMaxi();
        else WindowState|=FormWindowState.Maximized;
        return true;
       case Keys.Enter|Keys.Alt:
       case Keys.Enter|Keys.Control:
       case Keys.F11:
        ChangeMaxi();                     
        return true;
      }
      return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
      e.Graphics.FillRectangle(Brushes.Black,e.ClipRectangle);
    }
    protected override void OnPaint(PaintEventArgs e) {
      dirtydraw=true;
    }
    protected override void OnResize(EventArgs e) {
      base.OnResize(e);
      UpdateSize();      
    }
    void UpdateSize() {
      int cw=ClientRectangle.Width,ch=ClientRectangle.Height;
      int size=cw/lvl_w/img_wall.Width,sizey=ch/lvl_h/img_wall.Height;
      if(sizey<size) size=sizey;
      if(size<=0) size=1;
      scale=size;
      ww=size*img_wall.Width;
      hh=size*img_wall.Height;
      sx=(cw-lvl_w*ww)/2;
      sy=(ch-lvl_h*hh)/2;
      dirtydraw=true;
    }

    static void Draw(Graphics gr,Image img,int x,int y) { 
      DrawXY(gr,img,x*img_wall.Width,y*img_wall.Height);
    }
    static void DrawXY(Graphics gr,Image img,int x,int y) {
      gr.DrawImageUnscaled(img,x,y);
    }
    protected void DrawBack() {
      gr2.FillRectangle(Brushes.Black,0,0,bm2.Width,bm2.Height);
      int w2=img_wall.Width,h2=img_wall.Height;
      for(int y=0;y<lvl_h;y++)
        for(int x=0;x<lvl_w;x++) {
          Image img=CharImage(board[y*lvl_w+x]);
          gr2.DrawImageUnscaled(img_empty,x*w2,y*h2);
          if(img!=img_empty) gr2.DrawImageUnscaled(img,x*w2,y*h2);
        }
      gr2.DrawImageUnscaled(img_me,my_x*w2,my_y*h2);
      DrawFinalText();
      Graphics gr=this.CreateGraphics();
      CopyBlock(gr,0,0,lvl_w,lvl_h);
      //gr.DrawImage(bm2,sx,sy,lvl_w*ww,lvl_h*hh);  
      gr.Dispose();
    }
    
    void DrawFinalText() {
      if(string.IsNullOrEmpty(final_text)) return;            
      Font f=new Font("Tahoma",10,FontStyle.Bold);
      SizeF sz=gr2.MeasureString(final_text,f);
      int fw=(int)(sz.Width+0.99),fh=(int)(sz.Height+0.99);
      int rw=lvl_w*img_wall.Width-16;
      int rx=(lvl_w*img_wall.Width-rw)/2;
      int tx=(lvl_w*img_wall.Width-fw)/2;
      int ty=(lvl_h*img_wall.Height-fh)/2;
      if(tx<rx+2) tx=rx+2;
      gr2.FillRectangle(Brushes.Black,rx-2,ty-2,rw+4,fh+4);
      gr2.DrawRectangle(new Pen(Brushes.White,1),rx-2,ty-2,rw+4,fh+4);
      gr2.DrawString(final_text,f,final_brush,tx,ty);    
    }

    
    protected override void OnKeyUp(KeyEventArgs e) {
      switch(e.KeyCode) {
       case Keys.Up:go_up=false;go_up0=true;break;
       case Keys.Down:go_down=false;go_down0=true;break;
       case Keys.Left:go_left=false;go_left0=true;break;
       case Keys.Right:go_right=false;go_right0=true;break;
       case Keys.Menu:jump_1=false;break;
       case Keys.ControlKey:jump_2=false;break;
       case Keys.ShiftKey:jump_3=false;break;
      }
    }
    protected override void OnKeyDown(KeyEventArgs e) {
      switch(e.KeyCode) {
       case Keys.Up:go_up=true;go_up1=true;break;
       case Keys.Down:go_down=true;go_down1=true;break;
       case Keys.Left:go_left=true;go_left1=true;break;
       case Keys.Right:go_right=true;go_right1=true;break;
       case Keys.Menu:jump_1=true;break;
       case Keys.ControlKey:jump_2=true;break;
       case Keys.ShiftKey:jump_3=true;break;
       
      }
    }
    
    void try_key(int dx,int dy) {
      if(go_left&&dx>0) return;
      if(go_right&&dx<0) return;
      if(Level.IsWall(BoardXY(my_x+dx,my_y+dy))) return;
      go_clear();
      if(dx<0) go_left=true;else if(dx>0) go_right=true;
      if(dy<0) go_up=true;else if(dy>0) go_down=true;
    }    
    
    int GetShift(int x,int y,bool right) {
      int dx=right?1:-1;
      int sh=-1,nx=x;
      do {
        nx+=dx;
        if(nx<0||nx>=lvl_w) return 0;
        if(Level.IsWall(BoardXY(nx,y))) return 0;        
        sh++;
      } while(Level.IsSolidOrBlock(BoardXY(nx,y)));
      return sh;
    }

    void CopyBlock(Graphics gr,int x,int y,int w,int h) {
      Rectangle src=new Rectangle(),dst=new Rectangle();
      src.X=x*img_wall.Width;src.Y=y*img_wall.Height;src.Width=w*img_wall.Width;src.Height=h*img_wall.Height;
      dst.X=sx+x*ww;dst.Y=sy+y*hh;dst.Width=w*ww;dst.Height=h*hh;
      if(1==1||x>0) {src.X-=1;src.Width++;dst.X-=scale;dst.Width+=scale;}
      if(1==1||y>0) {src.Y-=1;src.Height++;dst.Y-=scale;dst.Height+=scale;}
      if(1==1||x+w<lvl_w) {src.Width+=1;dst.Width+=scale;}
      if(1==1||y+h<lvl_h) {src.Height+=1;dst.Height+=scale;}
      gr.DrawImage(bm2,dst,src,GraphicsUnit.Pixel);    
    }

    void ShowCursor(bool show) {
      if(cursor==show) return;      
      cursor=show;
      if(cursor) Cursor.Show();
      else Cursor.Hide();
    }
    

    protected override void OnMouseDown(MouseEventArgs e) {
      base.OnMouseDown(e);
      bool mous2=mous;
      mous=0!=(e.Button&MouseButtons.Left);
      if(mous) {
        if(!mous2) nomove=true;
        mous_x=(e.X-sx)/ww;mous_y=(e.Y-sy)/hh;
        bool maxi=WindowState==FormWindowState.Maximized;
        if(maxi) ShowCursor(true);
        if(stop) {
          if(full==0&&lvl<level.Count-1) lvl++;
          LoadLevel(lvl);
        } else if(start) start=false;
      }
    }
    protected override void OnMouseUp(MouseEventArgs e) {
      base.OnMouseUp(e);      
      if(0!=(e.Button&MouseButtons.Left)) {        
        if(mous&&nomove&&mous_x==my_x&&mous_y==my_y) {
          mous_xp=(e.X-sx)%ww*100/(ww-1)+1;        
          nomove=false;
        }
        mous=false;
      }
    }
    protected override void OnMouseMove(MouseEventArgs e){
      base.OnMouseMove(e);
      mous=0!=(e.Button&MouseButtons.Left);
      if(mous) {
        nomove=false;        
        mous_x=(e.X-sx)/ww;mous_y=(e.Y-sy)/hh;
      }
    }
    
    protected override void OnMouseDoubleClick(MouseEventArgs e) {
      if(mous_x==my_x&&mous_y==my_y)
        ChangeMaxi();
    }
    
    void ChangeMaxi() {
      bool maxi=!(WindowState==FormWindowState.Maximized);
      this.FormBorderStyle=maxi?FormBorderStyle.None:FormBorderStyle.Sizable;
      WindowState=maxi?FormWindowState.Maximized:FormWindowState.Normal;    
      ShowCursor(!maxi);      
    }
    
    void SetFinal() {
       int rec;
       bool isrec,ismin=false;
       if(!records.TryGetValue(lvl+1,out rec)) isrec=true;
       else {
         isrec=emptys<rec;
         ismin=emptys==rec;
       }
       if(isrec) {
         records[lvl+1]=emptys;
         SaveRecords(rec_file);
       }
       final_text="Level "+(lvl+1)+(isrec?" record":ismin?" best":" done")+" with "+emptys+(ismin||isrec&&emptys>rec?"":"("+(emptys-rec).ToString("+#;-#;")+")")+" moves!";
       final_brush=isrec?Brushes.Yellow:ismin?Brushes.Cyan:Brushes.White;    
    }

    internal void Lazy() {
      if(undos>0&&anim==0) Undo();
      if(redos>0&&anim==0) Redo();
      if(dirtydraw) {
        DrawBack();
        dirtydraw=false;
      }    
    }        
    private void timer1_Tick(object sender, EventArgs e) {
      tick++;
      if(start&&(go_left||go_right||go_up||go_down)) start=false;
      if(anim==0&&!start&&!stop) {        
        int x2=my_x,y2=my_y;
        shift=0;
        /*if(go_down) { y2=(my_y+1)%lvl_h;}
        else if(go_up) { y2=(my_y+lvl_h-1)%lvl_h;}
        else if(go_right) { x2=(my_x+1)%lvl_w;}
        else if(go_left) { x2=(my_x+lvl_w-1)%lvl_w;}
        go_clear();*/
        bool found=false;//(x2!=my_x||y2!=my_y)&&!Level.IsWall(BoardXY(x2,y2))&&(x2!=he_x||y2!=he_y);
        if(y2<lvl_h-1&&Level.IsEmpty(BoardXY(x2,y2+1))) { //fall
          found=true;
          y2++;moves--;//emptys++;
        }
        if(!found) {
          if(mous||mous_xp>0) {
            if(mous_x<my_x&&mous_y==my_y||mous_xp>0&&mous_xp<30) go_left1=true;            
            else if(mous_x>my_x&&mous_y==my_y||mous_xp>70) go_right1=true;
            else if(mous_y>my_y&&mous_x==my_x) go_down1=true;
            else if(mous_y<my_y&&mous_x==my_x) go_up1=true;
            else if(mous_x<my_x&&mous_y>=0&&mous_y==my_y-1) go_left1=go_up1=true;
            else if(mous_x>my_x&&mous_y>=0&&mous_y==my_y-1) go_right1=go_up1=true;
          }           
          Direction skip=Direction.None;
           for(int x=0;x<1;x++) {
            bool lshift=false,rshift=false;
            if((go_left||go_left1)&&0!=(shift=GetShift(x2,y2,false))) lshift=true;
            else if((go_right||go_right1)&&0!=(shift=GetShift(x2,y2,true))) rshift=true;
            if(y2>0&&Level.IsEmpty(BoardXY(x2,y2-1))&&(go_up||go_up1||jump_1||jump_2||jump_3||!(lshift||rshift))) {
              int dir=0; 
              if(x2>0&&(go_left||go_left1)&&Level.IsEmpty(BoardXY(x2-1,y2-1))&&!Level.IsEmpty(BoardXY(x2-1,y2))) dir=-1;
              else if(x2<lvl_w-1&&(go_right||go_right1)&&Level.IsEmpty(BoardXY(x2+1,y2-1))&&!Level.IsEmpty(BoardXY(x2+1,y2))) dir=1;
              if(dir!=0) {
                lshift=rshift=false;
                shift=0;
                if(y2<lvl_h-1&&Level.IsBlock(BoardXY(x2,y2+1))) {
                  UndoPush(UndoMode.Normal,x2,y2,x2+dir,y2-1,0,moves,emptys,full);
                  board[(y2+1)*lvl_w+x2]=Level.Empty;
                  full--;
                  if(full<1) stop=true;
                } else emptys++;
                y2--;
                x2+=dir;
                found=true;
                break;
              }
            }
            if(lshift||rshift) {
              if(lshift) shift=-shift;
              Shift(x2,y2,shift);
              UndoPush(UndoMode.Shift,x2,y2,x2,y2,shift,moves,emptys,full);
              break;
            }
            if((go_left||go_left1)&&x2>0&&Level.IsEmpty(BoardXY(x2-1,y2))) {
              if(y2<lvl_h-1&&Level.IsBlock(BoardXY(x2,y2+1))) {
                UndoPush(UndoMode.Normal,x2,y2,x2-1,y2,0,moves,emptys,full);
                board[(y2+1)*lvl_w+x2]=Level.Empty;
                full--;
                if(full<1) stop=true;
              } else emptys++;
              x2--;              
              found=true;
              break;
            }
            if((go_right||go_right1)&&x2<lvl_w-1&&Level.IsEmpty(BoardXY(x2+1,y2))) {
              if(y2<lvl_h-1&&Level.IsBlock(BoardXY(x2,y2+1))) {
                UndoPush(UndoMode.Normal,x2,y2,x2+1,y2,0,moves,emptys,full);
                board[(y2+1)*lvl_w+x2]=Level.Empty;
                full--;
                if(full<1) stop=true;
              } else emptys++;
              x2++;
              found=true;
              break;
            }
            if((go_up||go_up1)&&y2>0&&Level.IsSolidOrBlock(BoardXY(x2,y2-1))) {
              UndoPush(UndoMode.Up,x2,y2,x2,y2-1,0,moves,emptys,full);
              y2--;
              int idx=y2*lvl_w+x2;
              board[idx+lvl_w]=board[idx];
              board[idx]=Level.Empty;
              emptys++;
              found=true;
              break;
            }
            if((go_down||go_down1)&&y2<lvl_h-1&&Level.IsBlock(BoardXY(x2,y2+1))) {
              UndoPush(UndoMode.Normal,x2,y2,x2,y2+1,0,moves,emptys,full);
              y2++;
              board[y2*lvl_w+x2]=Level.Empty;
              full--;
              if(full<1) stop=true;
              found=true;
              break;
            }
          }
          mous_xp=0;
          go_up1=go_down1=go_left1=go_right1=false;
        }        
        if(shift!=0) {
          anim=anim_max;
          nomove=false;
        } else if(found) {
          nomove=false;
          my_x2=my_x;my_y2=my_y;
          my_x=x2;my_y=y2;
          anim=anim_max;
          moves++;
          Status(lvl,moves,emptys);
/*          SoundPlayer sound=isfull?full_sound:move_sound;
          if(sound!=null) {
            sound.Play();
          }*/
        }
      }
      if(anim>0) { //&&my_x2!=my_x||my_y2!=my_y||he_x2!=he_x||he_y2!=he_y) {
        
        int r=(anim_max+1-anim),r2=anim_max-r;  // r=1..anim_max,r2=anim_max-1..0

        int w2=img_wall.Width,h2=img_wall.Height;
        
        if(shift!=0) {
          bool right=shift>0;          
          int n=right?shift:-shift,dx=right?1:-1;
          //DrawXY(gr2,img_empty,(my_x+dx)*img_wall.Width,my_y*img_wall.Height);
          for(int i=0;i<=n;i++)
            DrawXY(gr2,img_empty,(my_x+dx*(i+1))*img_wall.Width,my_y*img_wall.Height);
          int drx,bx=my_x+2*dx;
          if(right) {
            drx=(my_x+dx)*img_wall.Width+r*img_wall.Width/anim_max;
          } else {
            drx=(my_x+2*dx)*img_wall.Width+r2*img_wall.Width/anim_max;
          }
          for(int i=0;i<n;i++) {
            DrawXY(gr2,CharImage(BoardXY(bx,my_y)),drx,my_y*img_wall.Height);
            drx+=dx*img_wall.Width;bx+=dx;
          }
        } else {
          Draw(gr2,img_empty,my_x,my_y);
          Draw(gr2,img_empty,my_x2,my_y2);
          int xx,yy;
          if(my_x!=my_x2&&my_y!=my_y2) {
            DrawXY(gr2,CharImage(BoardXY(my_x2,my_y)),my_x2*img_wall.Width,my_y*img_wall.Height);
            if(r<=anim_max/2) {
              xx=my_x2*img_wall.Width;yy=my_y2*img_wall.Height-(2*r)*img_wall.Height/anim_max;
            } else {
              xx=my_x2*img_wall.Width+(2*r-anim_max)*(my_x<my_x2?-1:1)*img_wall.Width/anim_max;yy=my_y*img_wall.Height;
            }
          } else {
            if(my_y<my_y2) DrawXY(gr2,CharImage(BoardXY(my_x2,my_y2)),my_x2*img_wall.Width,(r2*my_y+r*my_y2)*img_wall.Height/anim_max);
            xx=(r*my_x+r2*my_x2)*img_wall.Width/anim_max;yy=(r*my_y+r2*my_y2)*img_wall.Height/anim_max;
          }
          DrawXY(gr2,img_me,xx,yy);
          if(r2==0&&my_y<my_y2&&my_x==my_x2) {
            DrawXY(gr2,CharImage(BoardXY(my_x2,my_y2)),my_x2*img_wall.Width,my_y2*img_wall.Height);
          } else if(r2==0&&my_x!=my_x2&&my_y2<lvl_h-1) {
            DrawXY(gr2,CharImage(BoardXY(my_x2,my_y2+1)),my_x2*img_wall.Width,(my_y2+1)*img_wall.Height);
          }
        }
        
        Graphics gr=this.CreateGraphics();        
        Rectangle src=new Rectangle(),dst=new Rectangle();
        if(my_x!=my_x2||my_y!=my_y2) {
          CopyBlock(gr,my_x,my_y,1,1);  
          CopyBlock(gr,my_x2,my_y2,1,1);
          if(my_x!=my_x2&&my_y!=my_y2) {
            CopyBlock(gr,my_x2,my_y,1,1);
          }
          if(my_y2<lvl_h-1) {
            CopyBlock(gr,my_x2,my_y2+1,1,1);
          }  
        } else if(shift!=0) {
          int n=shift<0?-shift:shift,dx=shift<0?-1:1;
          if(shift<0) {
            src.X=(my_x+shift-1)*w2-1;src.Width=(-shift+1)*w2+2;
            dst.X=sx+(my_x+shift-1)*ww-scale;dst.Width=(-shift+1)*ww+2*scale;
          } else {
            src.X=(my_x+1)*w2-1;src.Width=(shift+1)*w2+2;
            dst.X=sx+(my_x+1)*ww-scale;dst.Width=(shift+1)*ww+2*scale;
          }
          src.Y=my_y*h2-1;src.Height=h2+2;
          dst.Y=sy+my_y*hh-scale;dst.Height=hh+2*scale;
          //gr.DrawImage(bm2,dst,src,GraphicsUnit.Pixel);       
          CopyBlock(gr,my_x+(dx<0?shift-1:1),my_y,n+1,1);   
        }
        
        

        if(r2==0) {
          my_x2=my_x;my_y2=my_y;
          shift=0;
          if(full==0) {
            SetFinal();
            DrawFinalText();
            gr.DrawImage(bm2,sx,sy,lvl_w*ww,lvl_h*hh);  
          }
        }
        gr.Dispose();
        anim--;
      }
    }
    
    static Direction Back(Direction dir) {
      return dir==Direction.Left?Direction.Right:dir==Direction.Right?Direction.Left:dir==Direction.Up?Direction.Down:Direction.Up;
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }
  }
  public enum Direction { None,Left=1,Right=2,Up=4,Down=8}  
  
  public static class Level {
    public const char Block='O';
    public const char Solid='S';
    public const char Wall='W';
    public const char Empty=' ';
    
    public static List<string> Load(string filename,out DateTime filetime) {
      List<string> level=new List<string>();
      TextReader tr=null;
     try {
      tr=new StreamReader(filename,System.Text.Encoding.Default);      
      string line,lvl=null;      
      while(null!=(line=tr.ReadLine())) {
        line=line.TrimEnd();
        bool empty=(line==""||0<=";-/+".IndexOf(line[0]));
        if(!empty) lvl+=(lvl==null?"":"\n")+line;
        else if(lvl!=null) {
          level.Add(lvl);
          lvl=null;
        }
      }
      if(lvl!=null) level.Add(lvl);
      filetime=File.GetLastWriteTimeUtc(filename);      
     } catch {
       filetime=DateTime.MinValue;
       level.Add(
@"XXXXXXXXXXXXXXXXXXXXXXXXXXXXX
X1                          X
X    XXXX XXX X    XXXX     X
X    X.   .X. X    X.       X
X    XXX   X  X    XXXX     X
X    X    .X. X    X.       X
X    X    XXX XXXX XXXX     X
X                           X
X XXXX XXX  XXX   XXX  XXX  X
X X.   X  X X  X X   X X  X X
X XXXX XXX. XXX. X   X XXX. X
X X.   X  X X  X X   X X  X X
X XXXX X  X X  X  XXX  X  X X
X!                          X
XXXXXXXXXXXXXXXXXXXXXXXXXXXXX"); 
     } finally {
      if(tr!=null) tr.Close(); 
     }
      return level;     
    }
    public static int Init(string level,ref int lvl_w,ref int lvl_h,ref char[] chars,ref int my_x,ref int my_y) {      
      List<string> line=new List<string>();
      lvl_w=0;
      int i2=0;
      for(int i=0;i<=level.Length;i++) {
        if(i==level.Length||level[i]=='\n') {
          string row=level.Substring(i2,i-i2-(i>i2+1&&level[i-1]=='\r'?1:0)).TrimEnd();
          if(row.Length>lvl_w) lvl_w=row.Length;
          line.Add(row);
          i2=i+1;
        }
      }
      lvl_h=line.Count;
      my_x=my_y=-1;
      int len=lvl_w*lvl_h;
      if(chars==null) chars=new char[len];
      else if(chars.Length<len) Array.Resize(ref chars,len);     
      int block=0;      
      for(int y=0;y<line.Count;y++) {
        string row=line[y];
        int x;
        for(x=0;x<row.Length;x++) {
          char rx=row[x];
          if(IsMy(rx)) { my_x=x;my_y=y;rx=Level.Empty;}
          if(IsBlock(rx)) block++;
          if(rx=='.'||rx==',') rx=Level.Empty;
          chars[y*lvl_w+x]=rx;
        }
        while(x<lvl_w) chars[y*lvl_w+x++]=Level.Empty;
      }
      return block;
    }
    public static bool IsWall(char ch) {
      return ch=='W'||ch=='w';
    }
    public static bool IsSolid(char ch) {
      return ch=='s'||ch=='S';
    }
    public static bool IsMy(char ch) {
      return ch=='i'||ch=='I';
    }
    public static bool IsBlock(char ch) {
      return ch=='O'||ch=='o';
    }
    public static bool IsSolidOrBlock(char ch) {
      return IsSolid(ch)||IsBlock(ch);
    }
    public static bool IsEmpty(char ch) {
      return char.IsWhiteSpace(ch);
    }
    
  }
  
}
