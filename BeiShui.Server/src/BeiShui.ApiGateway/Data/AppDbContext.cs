using Microsoft.EntityFrameworkCore;
using BeiShui.ApiGateway.Models;

namespace BeiShui.ApiGateway.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Rank> Ranks => Set<Rank>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
        public DbSet<Friend> Friends => Set<Friend>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<AntiCheatLog> AntiCheatLogs => Set<AntiCheatLog>();
        public DbSet<GameServer> GameServers => Set<GameServer>();
        public DbSet<DuelInvite> DuelInvites => Set<DuelInvite>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 用户表
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.SteamId).IsUnique();
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.MMR);
                entity.HasIndex(e => e.Status);
            });

            // 房间表
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.HasOne(e => e.Game).WithMany().HasForeignKey(e => e.GameId);
                entity.HasOne(e => e.HostUser).WithMany().HasForeignKey(e => e.HostUserId);
            });

            // 房间玩家
            modelBuilder.Entity<RoomPlayer>(entity =>
            {
                entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();
                entity.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // 对局
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasOne(e => e.Game).WithMany().HasForeignKey(e => e.GameId);
            });

            // 对局玩家
            modelBuilder.Entity<MatchPlayer>(entity =>
            {
                entity.HasOne(e => e.Match).WithMany().HasForeignKey(e => e.MatchId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // 好友关系
            modelBuilder.Entity<Friend>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.FriendId }).IsUnique();
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.FriendUser).WithMany().HasForeignKey(e => e.FriendId).OnDelete(DeleteBehavior.Restrict);
            });

            // 聊天消息
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasIndex(e => new { e.RoomId, e.CreatedAt });
                entity.HasIndex(e => new { e.FromUserId, e.ToUserId, e.CreatedAt });
                entity.HasOne(e => e.FromUser).WithMany().HasForeignKey(e => e.FromUserId);
            });

            // 反作弊日志
            modelBuilder.Entity<AntiCheatLog>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.MatchId);
            });

            // 游戏服务器
            modelBuilder.Entity<GameServer>(entity =>
            {
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.HasOne(e => e.HostUser).WithMany().HasForeignKey(e => e.HostUserId);
            });

            // 决斗邀约
            modelBuilder.Entity<DuelInvite>(entity =>
            {
                entity.HasOne(e => e.FromUser).WithMany().HasForeignKey(e => e.FromUserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ToUser).WithMany().HasForeignKey(e => e.ToUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // 语音房间
            modelBuilder.Entity<VoiceRoom>(entity =>
            {
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.HasOne(e => e.HostUser).WithMany().HasForeignKey(e => e.HostUserId);
            });

            // 语音房间成员
            modelBuilder.Entity<VoiceRoomMember>(entity =>
            {
                entity.HasIndex(e => new { e.RoomCode, e.UserId }).IsUnique();
            });

            // 节日数据
            modelBuilder.Entity<Holiday>().HasData(
                new Holiday { Id = 1, Name = "元旦", Date = "01-01", Message = "新的一年，在晨光中悄然来临。过去的三百六十五个日夜，有胜利的欢呼，也有失利的沉思。那些在服务器里度过的深夜，那些和队友并肩作战的瞬间，都已成为记忆里闪烁的星。愿新的一年，你的每一次瞄准都有归处，每一次冲锋都不孤单。新年快乐。", Color1 = "#FFD700", Color2 = "#FF6B6B", Emoji = "🎆" },
                new Holiday { Id = 2, Name = "春节", Date = "01-29", Message = "团圆，是中国人最朴素也最隆重的信仰。这一年，我们也许走了很远的路，在虚拟的世界里驰骋、战斗、拼搏。但这一刻，请放下键盘和鼠标，回到那个等你很久的家。窗外爆竹声声，桌上是母亲包的饺子，父亲斟满了一杯酒。愿家的温度，能温暖你一整年的奔波。新春快乐，平安喜乐。", Color1 = "#DC143C", Color2 = "#FF8C00", Emoji = "🧧" },
                new Holiday { Id = 3, Name = "元宵节", Date = "02-12", Message = "元宵的灯火，是这个春节最后的温柔。一碗热气腾腾的汤圆，咬开是甜，咽下是暖。也许你已经踏上归途的列车，也许你还在异乡的出租屋里，但请相信，那些思念的人终会相聚。元宵快乐，愿你的人生如这满街灯火，明亮而温暖。", Color1 = "#FF6B35", Color2 = "#FFD700", Emoji = "🏮" },
                new Holiday { Id = 4, Name = "母亲节", Date = "05-11", Message = "世界上有一种最动听的声音，那便是母亲的呼唤。她记得你爱吃的每一道菜，却从不提起自己的白发。她担心你熬夜打游戏伤眼睛，却只是默默在客厅留一盏灯。那些没说出口的爱，都藏在一粥一饭的日常里。今天，放下手中的游戏，给她打个电话吧。哪怕只是一句「妈，我在呢」。愿时光慢一点，让她老得慢一些。母亲节快乐。", Color1 = "#FF69B4", Color2 = "#FFB6C1", Emoji = "🌷" },
                new Holiday { Id = 5, Name = "父亲节", Date = "06-15", Message = "小时候，父亲的背影是一座山。他话不多，却总在你需要的时候出现。他可能不懂你玩的游戏，不懂什么叫CS2，什么叫爆头，但他知道你喜欢，就从不抱怨那台电脑的噪音。长大后你才发现，那个沉默的人，把所有的爱都藏在了行动里。这个父亲节，陪他下一盘棋，或者只是坐在一起喝杯茶。愿时光善待他。父亲节快乐。", Color1 = "#4169E1", Color2 = "#87CEEB", Emoji = "👔" },
                new Holiday { Id = 6, Name = "七夕", Date = "08-29", Message = "关于爱情，一千个人有一千种答案。也许是深夜双排时的默契配合，也许是残局中那句「别怕，有我」。也许是输了比赛后的一起复盘，也许是赢了之后隔着屏幕的笑声。最好的爱情，是你在哪，我就想去哪。愿你能遇见那个人，或者已经拥有那个人。七夕快乐。", Color1 = "#DA70D6", Color2 = "#FF69B4", Emoji = "💕" },
                new Holiday { Id = 7, Name = "中秋节", Date = "10-06", Message = "月亮圆了，就像我们心中那个关于团圆的念想。异乡的夜晚，你或许正独自坐在电脑前。窗外的月光和故乡的，其实是同一轮。那些你思念的人，也在同一轮明月下想着你。咬一口月饼，是甜的还是咸的？不重要。重要的是，有人和你一起吃着。中秋快乐，愿你月圆人团圆。", Color1 = "#FF8C00", Color2 = "#FFD700", Emoji = "🥮" },
                new Holiday { Id = 8, Name = "国庆节", Date = "10-01", Message = "十月金秋，红旗漫卷。我们生在红旗下，长在春风里。那些为这个国家付出过的人，他们的名字值得我们铭记。和平年代，我们能坐在电脑前享受游戏的快乐，不是因为世界本就和平，而是有人替我们挡住了风雨。这个国庆，如果可以，去看看那些历史，去走走那些风景，和重要的人一起。国庆快乐。", Color1 = "#FF0000", Color2 = "#FFD700", Emoji = "🇨🇳" },
                new Holiday { Id = 9, Name = "圣诞节", Date = "12-25", Message = "圣诞的钟声敲响时，窗外飘起了雪花。这一年快要结束了。你在这个世界里征战了多少个日夜？认识了多少朋友？又和多少人说了再见？圣诞老人或许不会真的到来，但温暖可以。一杯热可可，一条围巾，一句「圣诞快乐」。愿这个冬天，有人陪你度过。圣诞快乐，平安喜乐。", Color1 = "#DC143C", Color2 = "#228B22", Emoji = "🎄" },
                new Holiday { Id = 10, Name = "除夕", Date = "01-28", Message = "除夕，岁末的最后一天。过了今晚，就是新的一年。回头看，这一年走过的路，有高光时刻的酣畅淋漓，也有低谷时的咬牙坚持。每一场对局，每一次配合，都构成了独一无二的这一年。把所有的不开心都留在此刻吧。零点钟声响起时，新的故事就要开始了。愿新的一年，你依然热爱，依然勇敢，依然相信奇迹。除夕快乐，万事顺遂。", Color1 = "#FF4500", Color2 = "#FFD700", Emoji = "🎇" },
                new Holiday { Id = 11, Name = "重阳节", Date = "10-29", Message = "独在异乡为异客，每逢佳节倍思亲。王维写这句诗的时候，大概也是一个秋天的傍晚。重阳节，宜登高望远。山在那里，路在脚下。那些你思念的人，也许也在同一片天空下登高。插茱萸，饮菊花酒，愿时光善待每一位长辈。登高不只是为了看更远的风景，也是为了离想念的人更近一点。重阳安康。", Color1 = "#D2691E", Color2 = "#FFD700", Emoji = "🍁" }
            );

            // 公益数据
            modelBuilder.Entity<WelfareItem>().HasData(
                new WelfareItem { Id = 1, Title = "困境儿童的CS梦", Summary = "帮助山区孩子拥有自己的第一台电脑，打开通向世界的一扇窗。", Link = "https://www.iesdouyin.com/share/video/7618221279753861363/", Color1 = "#FF6B35", Color2 = "#FFD700", Icon = "🎮" },
                new WelfareItem { Id = 2, Title = "重阳敬老 · 爱在深秋", Summary = "陪伴是最长情的告白，关爱空巢老人，用温暖驱散孤独。", Link = "https://www.iesdouyin.com/share/video/7619318959103315826/", Color1 = "#D2691E", Color2 = "#FFD700", Icon = "🍁" }
            );

            // 种子数据：段位
            modelBuilder.Entity<Rank>().HasData(
                new Rank { Id = 1, Name = "青铜", MinMMR = 0, MaxMMR = 499, IconUrl = "/ranks/bronze.png" },
                new Rank { Id = 2, Name = "白银", MinMMR = 500, MaxMMR = 999, IconUrl = "/ranks/silver.png" },
                new Rank { Id = 3, Name = "黄金", MinMMR = 1000, MaxMMR = 1499, IconUrl = "/ranks/gold.png" },
                new Rank { Id = 4, Name = "铂金", MinMMR = 1500, MaxMMR = 1999, IconUrl = "/ranks/platinum.png" },
                new Rank { Id = 5, Name = "钻石", MinMMR = 2000, MaxMMR = 2499, IconUrl = "/ranks/diamond.png" },
                new Rank { Id = 6, Name = "大师", MinMMR = 2500, MaxMMR = 2999, IconUrl = "/ranks/master.png" },
                new Rank { Id = 7, Name = "宗师", MinMMR = 3000, MaxMMR = 9999, IconUrl = "/ranks/grandmaster.png" }
            );

            // 种子数据：游戏
            modelBuilder.Entity<Game>().HasData(
                new Game { Id = 1, Name = "Counter-Strike 2", ShortName = "cs2", ProcessName = "cs2.exe", LauncherArgs = "-appid 730" },
                new Game { Id = 2, Name = "PUBG: BATTLEGROUNDS", ShortName = "pubg", ProcessName = "TslGame.exe", LauncherArgs = "" },
                new Game { Id = 3, Name = "VALORANT", ShortName = "valorant", ProcessName = "VALORANT-Win64-Shipping.exe", LauncherArgs = "" },
                new Game { Id = 4, Name = "Apex Legends", ShortName = "apex", ProcessName = "r5apex.exe", LauncherArgs = "" }
            );
        }
    }
}
