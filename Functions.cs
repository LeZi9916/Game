﻿using RoguelikeGame.Class;
using RoguelikeGame.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static RoguelikeGame.GameResources;

namespace RoguelikeGame
{
    internal static partial class Game
    {
        internal const ConsoleColor Red = ConsoleColor.Red;
        internal const ConsoleColor Green = ConsoleColor.Green;
        internal const ConsoleColor Yellow = ConsoleColor.Yellow;
        internal const ConsoleColor White = ConsoleColor.White;

        static Func<Item, string> GetItemRarity = item => 
        {
            switch (item.Rarity) 
            {
                case RarityType.Common:
                    return "普通";
                case RarityType.Rare:
                    return "稀有";
                case RarityType.Epic:
                    return "史诗";
                default:
                    return "传奇";

            }
        };
        static Random rd = new();
        /// <summary>
        /// 表示当前Area信息
        /// </summary>
        internal static MapArea Area = new MapArea()
        {
            Type = AreaType.Plain,
            AreaStep = 90,
            PlayerStep = 0
        };
        /// <summary>
        /// 表示Player已走步数
        /// </summary>
        internal static int TotalStep = 0;
        internal static PrefabCollection Prefabs = new();//当前场景下的实体集合   
        

        /// <summary>
        /// 投骰子进行下一步
        /// </summary>
        /// <returns></returns>
        public static Result NextStep()
        {
            if (Area.AreaStep == Area.PlayerStep)
                return new Result()
                {
                    Type = ResultType.AreaFinish
                };

            var result = new Result();            
            var step = rd.Next(1,7);
            Area.PlayerStep += step;
            var remainingStep = Area.AreaStep - Area.PlayerStep;

            if (remainingStep <= 0)//已完成区域
            {
                result.Step = step + remainingStep;
                TotalStep += result.Step;
                Area.PlayerStep = Area.AreaStep;
                result.Type = ResultType.Boss;
                result.Monsters = CreateMonsters(MonsterType.Boss);
                return result;
            }
            else
            {
                result.Step = step;
                TotalStep += step;
                result.Type = WeightedRandom(new ResultType[]{ ResultType.Nothing,ResultType.Event,ResultType.Battle},new double[] {0.2,0.4,0.4});
                switch(result.Type)
                {
                    case ResultType.Event:
                        result.Event = RandomEvent();
                        break;
                    case ResultType.Battle:
                        result.Monsters = CreateMonsters(WeightedRandom(new MonsterType[] {MonsterType.Common,MonsterType.Elite },new double[] { 0.7,0.3 }));
                        break;
                }
                return result;
            }
        }
        /// <summary>
        /// 随机生成一个事件
        /// </summary>
        public static AreaEvent RandomEvent()
        {
            var eventType = WeightedRandom(new EventType[] {EventType.Adventure,EventType.Shop,EventType.Trap,EventType.Status},new double[] {0.4,0.1,0.4,0.1});
            var Events = EventList.Where(e => (e.Area == Area.Type || e.Area == AreaType.Common) && e.Type == eventType);

            return RandomChoose(Events.ToList());

        }
        /// <summary>
        /// 随机选择一个地图
        /// </summary>
        /// <returns></returns>
        public static AreaType NextArea()
        {
            var AreaTypes = new AreaType[] { AreaType.Desert, AreaType.City, AreaType.Icefield, AreaType.Grassland, AreaType.Plain, AreaType.Volcano };
            var areaType = RandomChoose(AreaTypes);
            double bonus = rd.Next(70,131) / 100;
            
            while (areaType == Area.Type )
                areaType = RandomChoose(AreaTypes);
            Area.AreaStep = (int)(90 * bonus);
            Area.Type = areaType;
            return areaType;
        }
        /// <summary>
        /// 区域事件对话处理
        /// </summary>
        /// <param name="e"></param>
        public static void EventHandle(AreaEvent e)
        {
            Clear();
            while(e.Type is EventType.Shop && Player.CoinCount <= 400)
                e = RandomEvent();
            List<Item> bonus;
            List<Item> failure;
            int epicCount, rareCount, commonCount, rdNum;
            char userInput;
            var coin = coinItem;
            

            if (e.Type is EventType.Adventure)
            {
                switch (e.Name)
                {
                    case "宝箱事件":
                        rdNum = rd.Next(0, 101);
                        WriteLine("     走着走着");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     突然间，你在道路旁发现了一个宝箱\n");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     A.打开它(说不定有奇珍异宝)", Yellow);
                        WriteLine("     B.算了吧(多一事不如少一事)", Green);
                        userInput = (char)Console.Read();
                        if (userInput == 'A')
                        {
                            WriteLine("     你激动地将宝箱打开了");
                            if (rdNum >= 50)//资源
                            {
                                rdNum = rd.Next(0, 101);
                                if (rdNum >= 50)//非空
                                {
                                    Console.ReadKey();
                                    //Thread.Sleep(2500);
                                    WriteLine("宝箱内部并发出一股紫色光芒");
                                    //Thread.Sleep(2500);
                                    Console.ReadKey();
                                    WriteLine("你的狗眼被亮瞎了");
                                    //Thread.Sleep(2500);
                                    Console.ReadKey();

                                    epicCount = WeightedRandom(new int[] { 1, 2, 3 }, new double[] { 0.85, 0.01, 0.05 });
                                    rareCount = WeightedRandom(new int[] { 2, 3, 4 }, new double[] { 0.7, 0.2, 0.1 });
                                    commonCount = rd.Next(3, 6);
                                    coin.Count = rd.Next(100, 400);

                                    bonus = new();
                                    failure = new();
                                    bonus.AddRange(RandomChoose(epicItems, epicCount));
                                    bonus.AddRange(RandomChoose(rareItems, rareCount));
                                    bonus.AddRange(RandomChoose(commonItems, commonCount));
                                    bonus.Add(coin);
                                    bonus.ForEach(item =>
                                    {
                                        if (Player.Items.Add(item))
                                            WriteLine($"     你获得了{item.Count} {item.Name}", Green);
                                        else
                                            failure.Add(item);
                                        FailAddItemUI(failure);
                                        Thread.Sleep(500);
                                    });

                                }
                                else//空
                                {
                                    WriteLine("     但事与愿违，宝箱里只有几枚陈旧的铜币");
                                    //Thread.Sleep(2500);
                                    Console.ReadKey();
                                    WriteLine("     你大失所望");
                                    coin = coinItem;
                                    coin.Count = rd.Next(40, 200);
                                    WriteLine($"     你获得了{coin.Count}枚{coin.Name}");
                                    Player.Items.Add(coin);
                                    Console.ReadKey();
                                }
                            }
                            else//战斗
                            {
                                var monsterCount = WeightedRandom(new int[] { 1, 2 }, new double[] { 0.9, 0.1 });
                                List<Monster> monsters = new(e.Monsters);
                                if (monsterCount == 2)
                                    monsters.AddRange(e.Monsters);
                                WriteLine("     突然间，人畜无害的宝箱毫无征兆地打开了", Yellow);
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     满是利齿的宝箱向你咬来", Red);
                                //Thread.Sleep(1500);
                                Console.ReadKey();
                                WriteLine("     你陷入了一场战斗!");
                                //战斗处理方法
                                BattleUI(monsters);
                            }
                        }
                        else
                        {
                            WriteLine("     你看了一眼亮闪闪的宝箱，没有留念，径直地离开了");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            if (rdNum >= 50)
                                WriteLine("     一阵风吹过，宝箱依旧在那，等待着下一个人将他开启...");
                            else
                                WriteLine("     宝箱突然颤动了一些，但没有任何人发现...");
                            Console.ReadKey();
                        }

                        break;

                    case "前辈":
                        WriteLine("     你目视前方");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     忽然间，你发现前方有一堆白色的反光物");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你决定走过去一探究竟");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     走近后，你发现那是冒险者前辈留下的遗物");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     为了继承他们的精神，你决定拾起他的遗物\n");
                        //Console.ReadKey();
                        Console.ReadKey();

                        rareCount = WeightedRandom(new int[] { 1, 2, 3 }, new double[] { 0.85, 0.1, 0.05 });
                        commonCount = rd.Next(1, 3);
                        coin.Count = rd.Next(10, 30);

                        bonus = new();
                        failure = new();
                        bonus.AddRange(RandomChoose(rareItems,rareCount));
                        bonus.AddRange(RandomChoose(commonItems,commonCount));
                        bonus.Add(coin);
                        bonus.ForEach(item => 
                        {
                            if (Player.Items.Add(item))
                                WriteLine($"     你获得了{item.Count} {item.Name}", Green);
                            else
                                failure.Add(item);
                            FailAddItemUI(failure);
                        });
                        Console.ReadKey();
                        break;

                    case "阿哈玩偶":

                        WriteLine("     你在路旁发现了一只三等身人形玩偶，它有稻草般的麻绳头发，肚子上有一个计数器");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     出于好奇，你把玩偶捡起并翻过来，上面写着:");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     「阿哈按照祂的样子制成的发泄玩偶——他希望看到自己被暴揍的样子。揍得越凶他越开心，就会给您越多通用货币!注:质量品控与阿哈本人无关。」\n");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     A. 轻轻拍他一下，至少不会弄坏他");
                        WriteLine("     B. 狠狠重击！不能被阿哈看扁了！");
                        Console.Read();

                        if(rd.Next(0, 101) < 40)//40%
                        {
                            WriteLine("     阿哈玩偶的计数器数字跳转，它达到了299。再来一下?你生活里需要发泄的地方太多了\n");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            WriteLine("     A. 轻轻拍他一下，至少不会弄坏他");
                            WriteLine("     B. 狠狠重击！不能被阿哈看扁了！");
                            if(rd.Next(0,101) < 20)
                            {
                                WriteLine("     计数器继续跳转，最后停留在了「400」上。玩偶内嵌芯片启动，传来一首颂歌「智识是坨废铁，存护是个呆子;毁灭像个疯子，阿哈真没面子!阿哈真没面子!阿哈真没面子! ......」");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你获得了400枚通用货币",Green);
                                coin.Count = 400;
                                Player.Items.Add(coin);
                            }
                            else
                            {
                                WriteLine("     阿哈玩偶的计数器疯狂跳转，它的数值膨胀至2147483647时突然跳转至了0,数据溢出了。你鄙视在如今使用32位int类型储存数据的制造商");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你一无所获");
                            }
                        }
                        else//60%
                        {
                            WriteLine("     阿哈玩偶的毫无反应，计数器上的数字没有跳转。你太逊了!\n");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            WriteLine("     A. 轻轻拍他一下，至少不会弄坏他");
                            WriteLine("     B. 狠狠重击！不能被阿哈看扁了！");
                            rdNum = rd.Next(0, 101);
                            if (rdNum < 10)
                            {
                                WriteLine("     计数器继续跳转，最后停留在了「400」上。玩偶内嵌芯片启动，传来一首颂歌「智识是坨废铁，存护是个呆子;毁灭像个疯子，阿哈真没面子!阿哈真没面子!阿哈真没面子! ......」");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你获得了400枚通用货币", Green);
                                coin.Count = 400;
                                Player.Items.Add(coin);
                            }
                            else if(rdNum < 30)
                            {
                                WriteLine("     阿哈玩偶被你打爆了——它碎成几块躺在地上。内嵌芯片启动，传来一首颂歌「智识是坨废铁，存护是个呆子;星神都一根筋，阿哈真没面子!阿哈真没面子!阿哈真没面子! .....」");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你一无所获");
                            }
                            else
                            {
                                WriteLine("     计数器继续跳转，最后停留在了「150」上。玩偶内嵌芯片启动，传来一首颂歌「智识是坨废铁，存护是个呆子;毁灭像个疯子，阿哈真没面子!阿哈真没面子!阿哈真没面子! ......」");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你获得了150枚通用货币", Green);
                                coin.Count = 150;
                                Player.Items.Add(coin);
                            }
                        }
                        Console.ReadKey();
                        break;

                    case "动物聚会":

                        WriteLine("     你在路边远远望去");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     发现一群动物聚在一起，像是在召开什么重要会议一般");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你想?\n");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     A. 上前查看（他们还能整什么幺蛾子出来）");
                        WriteLine("     B. 赶紧离开（看上去凶神恶煞，先走为敬）");
                        rdNum = rd.Next(0, 101);
                        userInput = (char)Console.Read();

                        if (userInput == 'A')
                        {
                            if(rdNum < 10)//10%
                            {
                                WriteLine("     你着急地跑过去，想要一探究竟");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     但发出的巨大动静把小猫吓到了，你被他挠了一下");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     小动物们慌不择路地跑开了");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                Player.Health -= (long)(Player.MaxHealth * 0.05);
                                WriteLine($"     你损失了{(long)(Player.MaxHealth * 0.05)}点生命值");
                            }
                            else if(rdNum < 40)//30%
                            {
                                WriteLine("     你着急地跑过去，想要一探究竟");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     但发出的巨大动静吓到了它们");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     小动物们慌不择路地跑开了");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你一无所获");
                            }
                            else 
                            {
                                WriteLine("     你轻手轻脚地走过去，想要一探究竟");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你被一只小松鼠发现了");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     小松鼠递给你一些松果");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你发现，松果里面夹杂着一些通用货币");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                WriteLine("     你喜出望外；恰好，动物们的聚会结束了");
                                //Thread.Sleep(2500);
                                Console.ReadKey();
                                coin.Count = rd.Next(20, 101);
                                WriteLine($"     获得了{coin.Count}枚通用货币");
                                Player.Items.Add(coin);
                            }
                        }
                        else
                            WriteLine("     为了不浪费自己的时间，你快步离开了");
                        Console.ReadKey();
                        break;
                }
                
            }
            else if (e.Type is EventType.Trap)
            {
                List<Monster> monsters;
                int count;
                switch(e.Name)
                {
                    case "史莱姆群":
                        monsters = new();
                        count = rd.Next(2, 5);
                        monsters.AddRange(RandomChoose(MonsterList.Search<Monster>("史莱姆"), count));

                        WriteLine("     突然间，一群史莱姆向你冲来");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你躲避不及");
                        Console.ReadKey();
                        //Thread.Sleep(2500);
                        WriteLine("     准备战斗！");
                        Console.ReadKey();
                        BattleUI(monsters);
                        break;
                    case "北极熊窝":
                        monsters = new(e.Monsters);
                        count = rd.Next(1, 3);
                        monsters.AddRange(RandomChoose(MonsterList[MonsterType.Common], count));
                        rdNum = rd.Next(0, 101);

                        WriteLine("     你发现前方有个洞穴");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     出于好奇，你进去洞穴一探究竟");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        if(rdNum < 50)
                        {
                            coin.Count = rd.Next(10, 51);
                            WriteLine("     你在洞穴里发现了一些通用货币");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            WriteLine($"     你获得了{coin.Count}枚通用货币", Green);
                            Player.Items.Add(coin);
                            Console.ReadKey();
                        }
                        else
                        {
                            WriteLine("     你在洞穴里面发现一只正在冬眠的北极熊");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            WriteLine("     打醒他?");
                            //Thread.Sleep(2500);
                            Console.ReadKey();
                            WriteLine("     Y. 打(干他娘的)");
                            WriteLine("     N. 别了吧(感觉不是很礼貌)");
                            if (Console.ReadLine() == "Y")
                                BattleUI(monsters);
                        }
                        break;
                    case "卫兵打劫":
                        monsters = new();
                        count = rd.Next(1, 3);
                        monsters.AddRange(RandomChoose(e.Monsters, count));
                        var price = rd.Next(50, 151);

                        WriteLine("     你在城市的道路上悠闲地散步");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     突然，你不小心撞到了城市卫兵");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     卫兵: 你小子不长眼的是吧？");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     卫兵: 看你脸生，给点赔偿金，我就就此揭过了");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     支付赔偿金？");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine($"     Y. 向卫兵支付{price}枚通用货币");
                        WriteLine("     N. 不给(这TM能忍?)");
                        if(Console.ReadLine() == "Y")
                        {
                            var money = Player.Items["通用货币"][0].Count;
                            var coinIndex = Player.Items.IndexOf(Player.Items["通用货币"][0]);
                            if (money < price)
                            {
                                WriteLine("     亲，你身上的货币不足以支付哦~");
                                Console.ReadLine();
                                BattleUI(monsters);
                            }
                            else
                            {
                                Player.Items[coinIndex].Count -= price;
                                WriteLine("     卫兵: 你小子还挺识相，快滚吧");
                                Console.ReadLine();
                            }
                        }
                        else
                            BattleUI(monsters);

                        break;
                }
            }
            else if (e.Type is EventType.Status)
            {
                switch(e.Name)
                {
                    case "灼伤":
                        WriteLine("     在行走的过程中，你发现了一处岩浆坑");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你好奇地凑近去查看");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     突然，一个小泡泡爆开了，几滴岩浆溅在你身上");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你疼得就地翻滚");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        Player.Health -= (long)(Player.MaxHealth * e.Value);
                        WriteLine($"     你损失了{(long)(Player.MaxHealth * e.Value)}点生命值");
                        Console.ReadKey();
                        break;

                    case "冻伤":
                        WriteLine("     一阵寒风过来");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     冷得你缩了缩脖子");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     长期的寒冷环境使得你多了几处冻疮");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你叫苦不迭");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        Player.Health -= (long)(Player.MaxHealth * e.Value);
                        WriteLine($"     你损失了{(long)(Player.MaxHealth * e.Value)}点生命值");
                        Console.ReadKey();
                        break;

                    case "缺水":
                        WriteLine("     烈日当头");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你浑身大汗，身上没有一处地方是干燥的");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     高强度的徒步冒险，再加上你无法及时补充水分");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     你已濒临极限");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        Player.Health -= (long)(Player.MaxHealth * e.Value);
                        WriteLine($"     你损失了{(long)(Player.MaxHealth * e.Value)}点生命值");
                        Console.ReadKey();
                        break;

                    case "昏厥":
                        WriteLine("     长期的高强图徒步使得你疲惫不堪");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     终于");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     身体无法继续支撑下去，你眼前一黑");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     晕过去了");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        Player.Health -= (long)(Player.MaxHealth * e.Value);
                        WriteLine($"     你损失了{(long)(Player.MaxHealth * e.Value)}点生命值");
                        Console.ReadKey();
                        break;
                }
            }
            else if (e.Type is EventType.Shop)
            {
                switch(e.Name)
                {
                    case "普通商人":
                        WriteLine("     你在城市中慢悠悠地散步");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     忽然间，你发现了一家商店，店老板在向你挥手");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     进去看看吗？");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     Y. 进去看看");
                        WriteLine("     N. 还是算了(钱包空空,腰带紧紧)");
                        if (Console.ReadLine() == "Y")
                            CommonShop();
                        break;
                    case "坎诺特":
                        WriteLine("     你遇到了坎诺特");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     坎诺特:最近新进了一批好货");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     要与坎诺特进行交易吗？");
                        //Thread.Sleep(2500);
                        Console.ReadKey();
                        WriteLine("     Y. 赶紧的赶紧的");
                        WriteLine("     N. 还是算了(看上去老不正经的)");
                        if (Console.ReadLine() == "Y")
                            KannutShop();
                        break;
                }
            }
        }
        /// <summary>
        /// 坎诺特商店货物生成方法
        /// </summary>
        public static void KannutShop()
        {
            int itemCount = 16;
            int legacyItemsCount = WeightedRandom(new int[] { 1, 2, 3 }, new double[] { 0.85, 0.1, 0.05 });
            int epicItemsCount = rd.Next(2, 5);
            int rareItemsCount = itemCount - legacyItemsCount - epicItemsCount;
            List<Item> shopItems = new();
            Dictionary<Item, int> itemPrice = new();

            shopItems.AddRange(RandomChoose(legacyItems, legacyItemsCount));
            shopItems.AddRange(RandomChoose(epicItems, epicItemsCount));
            shopItems.AddRange(RandomChoose(rareItems, rareItemsCount));
            shopItems.ForEach(item =>
            {
                switch (item.Rarity)
                {
                    case RarityType.Rare:
                        itemPrice.Add(item, rd.Next(50, 126));
                        break;
                    case RarityType.Epic:
                        itemPrice.Add(item, rd.Next(150, 351));
                        break;
                    case RarityType.Legacy:
                        itemPrice.Add(item, rd.Next(550, 901));
                        break;
                }
            });
            shopItems.OrderBy(item => item.Rarity);
            itemPrice.OrderBy(item => item.Key.Rarity);

            ShopUI(shopItems, itemPrice);
        }
        /// <summary>
        /// 普通商店的货物生成方法
        /// </summary>
        public static void CommonShop()
        {
            int itemCount = 8;
            int epicItemsCount = WeightedRandom(new int[] { 0,1,2 },new double[] { 0.85,0.1,0.05 });
            int rareItemsCount = rd.Next(0, 3);
            int commonItemsCount = itemCount - rareItemsCount - epicItemsCount;
            List<Item> shopItems = new();
            Dictionary<Item, int> itemPrice = new();

            shopItems.AddRange(RandomChoose(epicItems, epicItemsCount));
            shopItems.AddRange(RandomChoose(rareItems, rareItemsCount));
            shopItems.AddRange(RandomChoose(commonItems, commonItemsCount));
            shopItems.ForEach(item => 
            {
                switch(item.Rarity)
                {
                    case RarityType.Common:
                        itemPrice.Add(item, rd.Next(5, 41));
                        break;
                    case RarityType.Rare:
                        itemPrice.Add(item, rd.Next(50, 126));
                        break;
                    case RarityType.Epic:
                        itemPrice.Add(item, rd.Next(150, 351));
                        break;
                }
            });
            shopItems.OrderBy(item => item.Rarity);
            itemPrice.OrderBy(item => item.Key.Rarity);

            ShopUI(shopItems, itemPrice);            
        }
        /// <summary>
       /// 根据预设权重，随机生成指定数量的Item
       /// </summary>
       /// <param name="maxRank"></param>
       /// <param name="count"></param>
       /// <returns></returns>
        public static Item[] CreateItems(RarityType maxRank,int count)
        {
            const double legacyP = 0.02;
            const double epicP = 0.1;
            const double rareP = 0.20;
            const double commonP = 0.68;

            return CreateItems(maxRank, count, commonP, rareP, epicP, legacyP);
        }
        /// <summary>
        /// 根据权重，随机生成指定数量的Item
        /// </summary>
        /// <param name="maxRank"></param>
        /// <param name="count"></param>
        /// <param name="commonP"></param>
        /// <param name="rareP"></param>
        /// <param name="epicP"></param>
        /// <param name="legacyP"></param>
        /// <returns></returns>
        public static Item[] CreateItems(RarityType maxRank, int count,double commonP,double rareP,double epicP,double legacyP)
        {
            List<Item> items = new();
            while (items.Count != count)
            {
                var legacy = RandomChoose(legacyItems);
                var epic = RandomChoose(epicItems);
                var rare = RandomChoose(rareItems);
                var common = RandomChoose(commonItems);
                switch (maxRank)
                {
                    case RarityType.Legacy:
                        items.Add(WeightedRandom(new Item[] { common, rare, epic, legacy }, new double[] { commonP, rareP, epicP, legacyP }));
                        break;
                    case RarityType.Epic:
                        items.Add(WeightedRandom(new Item[] { common, rare, epic }, new double[] { commonP, rareP, epicP }));
                        break;
                    case RarityType.Rare:
                        items.Add(WeightedRandom(new Item[] { common, rare }, new double[] { commonP, rareP }));
                        break;
                    default:
                        items.Add(common);
                        break;
                }
            }
            return items.ToArray();
        }
        /// <summary>
        /// 随机生成一个或一组不超过指定阶级的Monster
        /// </summary>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public static Monster[] CreateMonsters(MonsterType maxRank) => CreateMonsters(maxRank, WeightedRandom(new int[] { 1, 2, 3, 4 }, new double[] { 0.445, 0.445, 0.1, 0.05 }));
        /// <summary>
        /// 随机生成指定数量不超过指定阶级的Monster
        /// </summary>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public static Monster[] CreateMonsters(MonsterType maxRank,int count)
        { 
            List<Monster> monsters;
            if(maxRank is MonsterType.Boss)
            {
                count = Math.Max(2, count);
                var boss = RandomChoose(MonsterList[MonsterType.Boss]);
                var eliteCount = Math.Min(count - 1, WeightedRandom(new int[] { 1, 2, 3 }, new double[] { 0.45, 0.35, 0.2 }));
                var commonCount = count - 1 - eliteCount;
                var elites = RandomChoose(MonsterList[MonsterType.Elite], eliteCount);
                var common = RandomChoose(MonsterList[MonsterType.Common], commonCount);
                monsters = new(elites)
                {
                    boss
                };
                monsters.AddRange(common);
                return monsters.ToArray();
            }
            else if(maxRank is MonsterType.Elite)
            {
                monsters = new(RandomChoose(MonsterList[MonsterType.Elite], rd.Next(1, count)));
                monsters.AddRange(RandomChoose(MonsterList[MonsterType.Common], count - monsters.Count));
                return monsters.ToArray();
            }
            else
                return RandomChoose(MonsterList[MonsterType.Elite], count);

        }
        /// <summary>
        /// 随机从Array中选取指定数量的对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static T[] RandomChoose<T>(IList<T> array,int count)
        {
            List<T> results = new();
            while (results.Count != count)
                results.Add(RandomChoose(array));
            return results.ToArray();
        }
        /// <summary>
        /// 随机从Array中选取一个对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T RandomChoose<T>(IList<T> array)
        {
            return array[new Random().Next(array.Count)];
        }
        /// <summary>
        /// 根据权重，随机从array中选取指定数量的对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pairs"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static T[] WeightedRandom<T>(IDictionary<T, double> pairs,int count) where T : notnull
        {
            List<T> results = new();
            while(results.Count != count)
                results.Add(WeightedRandom(pairs));
            return results.ToArray();
        }
        /// <summary>
        /// 根据权重，随机从array中选取指定数量的对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="weights"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static T[] WeightedRandom<T>(IList<T> array, double[] weights, int count) where T : notnull
        {
            List<T> results = new();
            while(results.Count != count)
                results.Add(WeightedRandom(array, weights));
            return results.ToArray();
        }
        /// <summary>
        /// 根据权重，随机从array中选取一个对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static T WeightedRandom<T>(IDictionary<T, double> pairs) where T : notnull
        {
            double totalWeight = pairs.Values.Sum();
            var randomDouble = new Random().NextDouble();
            var randomResult = totalWeight * randomDouble;
            foreach (var pair in pairs)
                if ((randomResult -= pair.Value) < 0)
                    return pair.Key;
            return pairs.Keys.Last();
        }
        /// <summary>
        /// 根据权重，随机从array中选取一个对象并返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static T WeightedRandom<T>(IList<T> array, double[] weights) where T : notnull
        {
            double totalWeight = weights.Sum();
            var randomResult = new Random().NextDouble() * totalWeight;
            foreach (var weight in weights)
                if ((randomResult -= weight) < 0)
                    return array[Array.IndexOf(weights,weight)];
            return array.Last();
        }
        public static void WriteLine(string text,ConsoleColor color)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = defaultColor;
        }
        public static void WriteLine(string text) => Console.WriteLine(text);
        public static void WriteLine() => Console.WriteLine();
        public static void Clear() 
        {
            Console.Clear();
            WriteLine("###################################################");
            WriteLine($"              Name      :{Player.Name}");
            WriteLine($"              Health    :{Player.Health}");
            WriteLine($"              Armor     :{Player.Armor}");
            WriteLine($"              Level     :{Player.Level}");
            WriteLine($"              NextLevel :{Player.ExpMaxLimit - Player.Experience}");
            WriteLine("###################################################");
        }
        public static string GetBase64Str(string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        public static string GetBase64Str(double input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input.ToString()));
        public static string GetBase64Str(float input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input.ToString()));
        public static string GetBase64Str(long input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input.ToString()));
        public static string Base64ToStr(string base64Str) => Encoding.UTF8.GetString(Convert.FromBase64String(base64Str)); 
    }
}
