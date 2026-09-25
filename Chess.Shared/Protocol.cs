using System.Text.Json;

namespace Chess.Shared;

public sealed class Packet
{
    public string Type { get; set; } = "";
    public string? RequestId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? GameId { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Text { get; set; }
    public string? Promotion { get; set; }
    public bool Accept { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Board { get; set; }
    public string? Turn { get; set; }
    public string? Opponent { get; set; }
    public string? Result { get; set; }
    public List<string>? Players { get; set; }
    public List<string>? Items { get; set; }
    public static Packet Reply(Packet request, bool success, string? error = null) => new() { Type = request.Type + "_RESPONSE", RequestId = request.RequestId, Success = success, Error = error };
}

public static class Wire
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    public static string Encode(Packet p) => JsonSerializer.Serialize(p, Options);
    public static Packet? Decode(string line) => JsonSerializer.Deserialize<Packet>(line, Options);
}

// Compact, teachable rules engine: legal movement, check, mate, stalemate and promotion.
public sealed class Board
{
    public char[,] Squares { get; } = new char[8,8];
    public char Turn { get; private set; } = 'w';
    public Board() { const string back="rnbqkbnr"; for(int x=0;x<8;x++){Squares[x,0]=back[x];Squares[x,1]='p';Squares[x,6]='P';Squares[x,7]=char.ToUpperInvariant(back[x]);} }
    public string Serialize() { var a=new char[64]; for(int y=0;y<8;y++)for(int x=0;x<8;x++)a[y*8+x]=Squares[x,y]==0?'.':Squares[x,y]; return new string(a); }
    public bool Move(string? from,string? to,string? promotion,out string status)
    {
        status="Illegal move."; if(!Square(from,out int fx,out int fy)||!Square(to,out int tx,out int ty)||fx==tx&&fy==ty)return false;
        char p=Squares[fx,fy], target=Squares[tx,ty]; if(p==0||Color(p)!=Turn||target!=0&&Color(target)==Turn)return false;
        if(!Pseudo(fx,fy,tx,ty))return false; char old=target; Squares[tx,ty]=p;Squares[fx,fy]=(char)0;
        if(InCheck(Turn)){Squares[fx,fy]=p;Squares[tx,ty]=old;return false;}
        if(char.ToLowerInvariant(p)=='p'&&(ty==0||ty==7)) Squares[tx,ty]=Turn=='w'?char.ToUpperInvariant((promotion??"q")[0]):char.ToLowerInvariant((promotion??"q")[0]);
        Turn=Turn=='w'?'b':'w'; bool check=InCheck(Turn), has=HasLegalMove(Turn); status=check?(has?"Check":"Checkmate"):(has?"Move accepted":"Stalemate"); return true;
    }
    private static bool Square(string? s,out int x,out int y){x=y=0;if(s==null||s.Length!=2||s[0]<'a'||s[0]>'h'||s[1]<'1'||s[1]>'8')return false;x=s[0]-'a';y=8-(s[1]-'0');return true;}
    private char Color(char p)=>char.IsUpper(p)?'w':'b';
    private bool Pseudo(int x,int y,int a,int b){char p=char.ToLowerInvariant(Squares[x,y]);int dx=a-x,dy=b-y; if(p=='n')return Math.Abs(dx)*Math.Abs(dy)==2;
        if(p=='k')return Math.Max(Math.Abs(dx),Math.Abs(dy))==1;
        if(p=='p'){int d=Color(Squares[x,y])=='w'?-1:1; if(dx==0&&Squares[a,b]==0&&(dy==d||y==(d<0?6:1)&&dy==2*d&&Squares[x,y+d]==0))return true;return Math.Abs(dx)==1&&dy==d&&Squares[a,b]!=0;}
        bool diag=Math.Abs(dx)==Math.Abs(dy), straight=dx==0||dy==0; if(p=='b'&&!diag||p=='r'&&!straight||p=='q'&&!diag&&!straight)return false;
        int sx=Math.Sign(dx),sy=Math.Sign(dy),cx=x+sx,cy=y+sy;while(cx!=a||cy!=b){if(Squares[cx,cy]!=0)return false;cx+=sx;cy+=sy;}return true; }
    private bool InCheck(char color){int kx=-1,ky=-1; for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(Squares[x,y]==(color=='w'?'K':'k')){kx=x;ky=y;} if(kx<0)return true;
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(Squares[x,y]!=0&&Color(Squares[x,y])!=color&&Pseudo(x,y,kx,ky))return true;return false;}
    private bool HasLegalMove(char color){for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(Squares[x,y]!=0&&Color(Squares[x,y])==color)for(int a=0;a<8;a++)for(int b=0;b<8;b++)if(Pseudo(x,y,a,b)){char p=Squares[x,y],t=Squares[a,b];Squares[a,b]=p;Squares[x,y]=(char)0;bool ok=!InCheck(color);Squares[x,y]=p;Squares[a,b]=t;if(ok)return true;}return false;}
}
