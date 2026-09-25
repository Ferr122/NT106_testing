using System.Net.Sockets;
using System.Text;
using Chess.Shared;

namespace Chess.Client;
public sealed class ChessForm : Form
{
    private readonly TextBox txtUsername=new(){Name="txtUsername",PlaceholderText="Username"},txtPassword=new(){Name="txtPassword",PlaceholderText="Password",UseSystemPasswordChar=true};
    private readonly TextBox txtChat=new(){Name="txtChat",Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    private readonly TextBox txtMessage=new(){Name="txtMessage"};
    private readonly Button btnLogin=new(){Name="btnLogin",Text="Log in"},btnRegister=new(){Name="btnRegister",Text="Register"},btnMatch=new(){Name="btnMatch",Text="Find opponent"},btnHistory=new(){Text="History"},btnLeaderboard=new(){Text="Leaderboard"},btnResign=new(){Text="Resign"},btnDraw=new(){Text="Offer draw"},btnChat=new(){Name="btnSend",Text="Send"};
    private readonly Label lblStatus=new(){AutoSize=true,Text="Connect to server: localhost:5050"};
    private readonly Button[,] squares=new Button[8,8];
    private TcpClient? tcp; private StreamReader? reader; private StreamWriter? writer; private string? user,game; private char side='w'; private string? selected;
    public ChessForm()
    {
        Text="Online Chess — Student Project";Width=1020;Height=720;MinimumSize=new(900,650);StartPosition=FormStartPosition.CenterScreen;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)};root.ColumnStyles.Add(new(SizeType.Percent,65));root.ColumnStyles.Add(new(SizeType.Percent,35));root.RowStyles.Add(new(SizeType.Absolute,90));root.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(root);
        var auth=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};auth.Controls.AddRange([txtUsername,txtPassword,btnLogin,btnRegister,btnMatch,btnHistory,btnLeaderboard,btnDraw,btnResign,lblStatus]);root.Controls.Add(auth,0,0);root.SetColumnSpan(auth,2);
        txtUsername.Width=140;txtPassword.Width=140;
        var board=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=8,ColumnCount=8,CellBorderStyle=TableLayoutPanelCellBorderStyle.Single};for(int i=0;i<8;i++){board.RowStyles.Add(new(SizeType.Percent,12.5f));board.ColumnStyles.Add(new(SizeType.Percent,12.5f));}
        const string initial="rnbqkbnrpppppppp................................PPPPPPPPRNBQKBNR";for(int y=0;y<8;y++)for(int x=0;x<8;x++){int xx=x,yy=y;var b=new Button{Name=$"sq_{(char)('a'+x)}{8-y}",Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI Symbol",22,FontStyle.Bold),BackColor=(x+y)%2==0?Color.FromArgb(238,216,180):Color.FromArgb(119,149,86),Margin=Padding.Empty};b.Click+=async(_,_)=>await SquareClick(xx,yy);squares[x,y]=b;board.Controls.Add(b,x,y);}root.Controls.Add(board,0,1);
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3};right.RowStyles.Add(new(SizeType.Absolute,34));right.RowStyles.Add(new(SizeType.Percent,100));right.RowStyles.Add(new(SizeType.Absolute,36));right.Controls.Add(new Label{Text="Lobby and match chat",Dock=DockStyle.Fill,Font=new Font(Font,FontStyle.Bold)},0,0);right.Controls.Add(txtChat,0,1);var send=new FlowLayoutPanel{Dock=DockStyle.Fill};txtMessage.Width=190;send.Controls.AddRange([txtMessage,btnChat]);right.Controls.Add(send,0,2);root.Controls.Add(right,1,1);
        btnLogin.Click+=async(_,_)=>await Authenticate("LOGIN");btnRegister.Click+=async(_,_)=>await Authenticate("REGISTER");btnMatch.Click+=async(_,_)=>await Send(new Packet{Type="JOIN_MATCHMAKING"});btnChat.Click+=async(_,_)=>await Chat();btnHistory.Click+=async(_,_)=>await Send(new Packet{Type="GET_HISTORY"});btnLeaderboard.Click+=async(_,_)=>await Send(new Packet{Type="GET_LEADERBOARD"});btnResign.Click+=async(_,_)=>await Send(new Packet{Type="RESIGN",GameId=game});btnDraw.Click+=async(_,_)=>await Send(new Packet{Type="DRAW_REQUEST",GameId=game});PaintBoard(initial);
    }
    private async Task Connect(){if(tcp?.Connected==true)return;tcp=new TcpClient();await tcp.ConnectAsync("127.0.0.1",5050);reader=new StreamReader(tcp.GetStream(),Encoding.UTF8);writer=new StreamWriter(tcp.GetStream(),new UTF8Encoding(false)){AutoFlush=true};_ = Task.Run(ReadLoop);}
    private async Task Authenticate(string type){try{await Connect();await Send(new Packet{Type=type,Username=txtUsername.Text.Trim(),Password=txtPassword.Text});}catch(Exception e){Status("Server unavailable: "+e.Message);}}
    private async Task Send(Packet p){if(writer==null)return;await writer.WriteLineAsync(Wire.Encode(p));}
    private async Task ReadLoop(){try{while(reader!=null){var line=await reader.ReadLineAsync();if(line==null)break;var p=Wire.Decode(line);if(p!=null)BeginInvoke(()=>HandleMessage(p));}}catch(Exception e){BeginInvoke(()=>Status("Disconnected: "+e.Message));}}
    private void HandleMessage(Packet p){if(p.Type.EndsWith("_RESPONSE")){if(!p.Success){Status(p.Error??"Request failed");return;}if(p.Type is "LOGIN_RESPONSE" or "REGISTER_RESPONSE"){user=txtUsername.Text;Status("Signed in as "+user);}else if(p.Type=="JOIN_MATCHMAKING_RESPONSE")Status("Waiting for an opponent…");else if(p.Type is "GET_HISTORY_RESPONSE" or "GET_LEADERBOARD_RESPONSE")MessageBox.Show(this,string.Join(Environment.NewLine,p.Items??[]),p.Type.StartsWith("GET_HISTORY")?"Match history":"Leaderboard");return;}
        if(p.Type=="MATCH_FOUND"){game=p.GameId;side=p.Result=="white"?'w':'b';Status($"Matched {p.Opponent}. You play {(side=='w'?"White":"Black")}");PaintBoard(p.Board??"");}
        if(p.Type=="GAME_STATE"){PaintBoard(p.Board??"");Status(p.Result??$"Turn: {p.Turn}");if(p.Result is "Checkmate" or "Stalemate")MessageBox.Show(this,p.Result,"Game finished");}
        if(p.Type=="CHAT_MESSAGE")txtChat.AppendText($"{p.Username}: {p.Text}{Environment.NewLine}");if(p.Type=="GAME_END")MessageBox.Show(this,p.Result??"Game ended");if(p.Type=="DRAW_REQUEST"){var accept=MessageBox.Show(this,$"{p.Username} offers a draw. Accept?","Draw offer",MessageBoxButtons.YesNo)==DialogResult.Yes;_=Send(new Packet{Type="DRAW_RESPONSE",GameId=game,Accept=accept});}}
    private void PaintBoard(string board){if(board.Length!=64)return;for(int y=0;y<8;y++)for(int x=0;x<8;x++){char c=board[y*8+x];squares[x,y].Text=c switch{'r'=>"♜",'n'=>"♞",'b'=>"♝",'q'=>"♛",'k'=>"♚",'p'=>"♟",'R'=>"♖",'N'=>"♘",'B'=>"♗",'Q'=>"♕",'K'=>"♔",'P'=>"♙",_=>""};}}
    private async Task SquareClick(int x,int y){if(game==null)return;string sq=$"{(char)('a'+x)}{8-y}";if(selected==null){selected=sq;return;}string from=selected;selected=null;await Send(new Packet{Type="MOVE",GameId=game,From=from,To=sq});}
    private async Task Chat(){if(string.IsNullOrWhiteSpace(txtMessage.Text))return;await Send(new Packet{Type="CHAT",GameId=game,Text=txtMessage.Text.Trim()});txtMessage.Clear();}
    private void Status(string text)=>lblStatus.Text=text;
}
