using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace SebastianSpending
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private Random random;
        private int DaysMarried => Game1.player.friendshipData.ContainsKey("Sebastian") 
            ? Game1.player.friendshipData["Sebastian"].DaysMarried 
            : 0;
        private bool HasChildren => Game1.player.getChildrenCount() > 0;
        private int ChildCount => Game1.player.getChildrenCount();

        // ========== 花费区间 ==========
        private readonly Dictionary<string, int[]> SpendingRanges = new()
        {
            // 新婚期（自己）
            ["gaming_newlywed"] = new[] { 200, 500, 1000 },
            ["coding_newlywed"] = new[] { 150, 400, 800 },
            ["snacks_newlywed"] = new[] { 50, 150, 300 },
            ["music_newlywed"] = new[] { 100, 250, 500 },
            
            // 新婚期（给你礼物）
            ["gift_crystal_newlywed"] = new[] { 100, 300, 600 },
            ["gift_tech_newlywed"] = new[] { 300, 700, 1200 },
            ["gift_comfort_newlywed"] = new[] { 150, 400, 700 },
            ["gift_surprise_newlywed"] = new[] { 80, 200, 400 },
            
            // 婚后稳定期（自己）
            ["gaming_married"] = new[] { 150, 400, 800 },
            ["coding_married"] = new[] { 100, 300, 600 },
            ["snacks_married"] = new[] { 40, 120, 250 },
            ["music_married"] = new[] { 80, 200, 450 },
            ["home_married"] = new[] { 200, 500, 900 },
            
            // 婚后稳定期（给你礼物）—— 更浪漫，更贵
            ["gift_crystal_married"] = new[] { 150, 400, 800 },
            ["gift_tech_married"] = new[] { 300, 800, 1500 },
            ["gift_comfort_married"] = new[] { 200, 500, 900 },
            ["gift_surprise_married"] = new[] { 100, 300, 600 },
            ["gift_romantic_married"] = new[] { 250, 600, 1200 }, // 新增：浪漫专属
            
            // 有孩子后（自己）
            ["gaming_parent"] = new[] { 100, 300, 600 },
            ["coding_parent"] = new[] { 80, 250, 500 },
            ["snacks_parent"] = new[] { 30, 80, 180 },
            ["music_parent"] = new[] { 60, 150, 350 },
            ["home_parent"] = new[] { 300, 700, 1200 },
            
            // 有孩子后（给你礼物）—— 仍然浪漫，你是他的妻子
            ["gift_crystal_parent"] = new[] { 120, 350, 700 },
            ["gift_tech_parent"] = new[] { 250, 700, 1400 },
            ["gift_comfort_parent"] = new[] { 180, 450, 800 },
            ["gift_surprise_parent"] = new[] { 80, 250, 500 },
            ["gift_romantic_parent"] = new[] { 300, 800, 1600 }, // 浪漫升级
            
            // 有孩子后（给孩子）
            ["child_toy"] = new[] { 50, 150, 300 },
            ["child_book"] = new[] { 80, 200, 400 },
            ["child_clothes"] = new[] { 100, 250, 500 },
            ["child_food"] = new[] { 40, 100, 200 },
            ["child_education"] = new[] { 200, 500, 1000 }
        };

        // ========== 礼物物品ID ==========
        private readonly Dictionary<string, int[]> GiftItems = new()
        {
            ["gift_crystal"] = new[] { 86, 84, 82, 80, 74, 72, 70, 68 },
            ["gift_tech"] = new[] { 787, 130, 645, 787 },
            ["gift_comfort"] = new[] { 395, 373, 341, 342 },
            ["gift_surprise"] = new[] { 221, 220, 72, 16, 18 },
            ["gift_romantic"] = new[] { 220, 221, 222, 223, 232 }, // 粉红蛋糕、巧克力蛋糕、星之果实等
            ["child_toy"] = new[] { 103, 104, 105 },
            ["child_book"] = new[] { 102, 770 },
            ["child_clothes"] = new[] { 428, 440 },
            ["child_food"] = new[] { 221, 220, 206 },
            ["child_education"] = new[] { 72, 74, 767 }
        };

        // ========== 对话池 ==========
        private readonly Dictionary<string, string[]> Dialogues = new()
        {
            // ===== 新婚期（0-30天）=====
            ["gaming_newlywed"] = new[]
            {
                "……我升级了一下电脑配置。别紧张，不是乱花钱，是之前的显卡真的不够用了。",
                "在网上看到一款老游戏的限定版，没忍住。我会控制预算的，放心。",
                "买了新的机械键盘……打字手感很好。你要试试吗？"
            },
            ["coding_newlywed"] = new[]
            {
                "订了一本关于人工智能的书，挺贵的，但内容很前沿。",
                "买了些开发工具的授权。算是……投资未来吧。",
                "在网上报了一个编程课程，晚上会花点时间看。你不会介意吧？"
            },
            ["snacks_newlywed"] = new[]
            {
                "去超市补了点货……冷冻披萨和Joja可乐。我知道不健康，但方便。",
                "买了些能量饮料，最近晚上想多写点代码。我会注意身体的。",
                "……偷偷买了包你上次说好吃的薯片。我也尝尝看。"
            },
            ["music_newlywed"] = new[]
            {
                "淘到了一张绝版的黑胶，等下放给你听。你应该会喜欢这首。",
                "买了新的吉他弦，还有一本和弦谱。想写首歌……还没想好主题。",
                "在网上发现了一个独立乐队，买了他们的专辑。晚上一起听？"
            },

            ["gift_crystal_newlywed"] = new[]
            {
                "……我去矿洞附近转了转，看到这个觉得你会喜欢。不是特意买的，就是顺手。",
                "在店里看到这块石头，颜色很像你眼睛。……别笑，我是认真的。",
                "我记得你说过喜欢收集这些。拿着吧，反正我留着也没用。"
            },
            ["gift_tech_newlywed"] = new[]
            {
                "看到这个觉得你可能用得上。……不是关心你，就是觉得家里应该有一个。",
                "网上打折，顺手买了。你不是说想要这个很久了吗？我没记错吧。",
                "……别多想，就是看到它的时候第一个想到你而已。"
            },
            ["gift_comfort_newlywed"] = new[]
            {
                "冬天要来了，给你买了这个。……你自己不会买，我只能代劳了。",
                "你最近好像很累。这个……应该能让你舒服一点。",
                "我在沙发上看到你裹着旧毯子发抖。新的，给你。不准说不要。"
            },
            ["gift_surprise_newlywed"] = new[]
            {
                "……路过沙龙的时候看到的。你不是喜欢甜的吗。拿着。",
                "这个，给你。没什么特别的理由。就是想看你笑一下。",
                "我很少做这种事。但今天是……算了，你收下就好。"
            },

            // ===== 婚后稳定期（31天+，无孩子）—— 更浪漫，更偏向你 =====
            ["gaming_married"] = new[]
            {
                "电脑有点卡，买了个小升级。……没花多少，真的。",
                "Steam打折，入了几款独立游戏。晚上一起玩？",
                "键盘空格键坏了，换了新的。旧的用了三年，该换了。"
            },
            ["coding_married"] = new[]
            {
                "续费了开发工具的订阅。……是必需品，不是乱花。",
                "买了本关于农场自动化的书。想着……能不能帮你写点代码。",
                "网上有个编程马拉松，报名费我交了。奖金够买台新电脑。"
            },
            ["snacks_married"] = new[]
            {
                "超市冷冻披萨买一送一，我囤了点。……不是我想吃，是划算。",
                "Joja可乐出了限定口味。我就买了一瓶，尝尝。",
                "买了些咖啡豆，早上自己磨。比沙龙便宜多了。"
            },
            ["music_married"] = new[]
            {
                "买了张二手黑胶，有点划痕但还能听。等下放给你？",
                "吉他弦又断了，批量买了几套。这次应该够用半年。",
                "发现了个地下乐队，买了他们的数字专辑。分享给你。"
            },
            ["home_married"] = new[]
            {
                "……我买了盏台灯。你晚上看书光线太暗，对眼睛不好。",
                "换了浴室的浴帘。旧的霉了，我没告诉你而已。",
                "买了个小书架，你的书堆得到处都是。……不是抱怨，是关心。"
            },

            // 婚后给你买礼物——浪漫升级，你是他最重要的人
            ["gift_crystal_married"] = new[]
            {
                "矿洞里看到的，顺手捡了。……好吧是买的，但觉得你会喜欢。",
                "镇上古董店淘的，说是有'能量'。艾米丽说的，不是我。",
                "这个月的'随便看到就买了'。给你。"
            },
            ["gift_tech_married"] = new[]
            {
                "你手机充电线又断了，我买了根结实的。……别问我怎么知道的。",
                "买了个小风扇，夏天放床头。你晚上总说热。",
                "这个工具……你农场用得上的吧？我用不上，给你。"
            },
            ["gift_comfort_married"] = new[]
            {
                "新拖鞋，旧的底磨平了。……我观察过，你走路姿势变了。",
                "买了条厚袜子，冬天下地干活穿。不准说丑。",
                "这个靠垫……你腰不好，坐着干活时用。"
            },
            ["gift_surprise_married"] = new[]
            {
                "路过沙龙，蛋糕剩最后一块。……给你带的，我不吃甜的。",
                "看到了这个，莫名其妙想到你。拿着吧。",
                "今天……没什么特别。就是想给你买点什么。"
            },
            ["gift_romantic_married"] = new[]
            {
                "……我订了餐厅。今晚别做饭了，我带你出去。就我们俩。",
                "买了瓶葡萄酒，你上次说想试试的。晚上……晚点睡？",
                "这个项链，我看到的时候就知道必须是你戴。……转过去，我帮你扣。",
                "我写了首歌。……还没练好，但这个歌词本先给你看。别笑。",
                "订了两张去山区的车票。周末……只有我们两个人。农场让罗宾照看一下。"
            },

            // ===== 有孩子后 —— 你是他的妻子，不是"孩子的妈妈" =====
            ["gaming_parent"] = new[]
            {
                "……买了款打折游戏。就一款，真的。别的都没买。",
                "键盘有个键不灵了，换了单个轴体。没买新的，省钱。",
                "朋友送了张二手显卡，我请他吃了顿饭。……比买新的便宜。"
            },
            ["coding_parent"] = new[]
            {
                "买了本电子书，比纸质便宜一半。……我在学省钱了。",
                "续费了最便宜的开发工具套餐。够用了，不用高级的。",
                "网上免费课程够多了，没花钱。……真的。"
            },
            ["snacks_parent"] = new[]
            {
                "……买了包胡萝卜条。Joja可乐没买，我戒了。",
                "零食换成了燕麦棒，健康还便宜。孩子也能吃。",
                "咖啡改自己手冲了，不买沙龙的了。省下的钱……给你买礼物。"
            },
            ["music_parent"] = new[]
            {
                "买了套便宜的吉他弦，国产的。音色差不多。",
                "用免费软件做了首歌，没买新插件。……你要听吗？是写给你的。",
                "黑胶不买了，数字版也一样。但给你买礼物不能省。"
            },
            ["home_parent"] = new[]
            {
                "买了婴儿护栏，孩子开始爬了。……我装的，很结实。",
                "换了防触电插座。安全第一，这个不能省。",
                "买了加湿器，冬天太干对孩子不好。你也舒服点。",
                "……我修了漏水的水龙头，没请工人。零件花了点钱，但比人工便宜。"
            },

            // 有孩子后给你买礼物——你仍然是他爱的人，不只是母亲
            ["gift_crystal_parent"] = new[]
            {
                "……去镇上顺便买的。孩子睡了，我们看看这个？",
                "矿洞边的店要关门了，清仓买的。便宜，但挺好看。",
                "这个给你。孩子抓着我的手选的……好吧是我选的，但孩子笑了是因为看到你开心。"
            },
            ["gift_tech_parent"] = new[]
            {
                "你手机内存满了，我买了张存储卡。……照片我都备份了，放心。",
                "买了个小夜灯，孩子晚上哭你能看清路。……也是给你的，你怕黑。",
                "这个按摩器，你抱孩子腰肯定酸了。……我用过，还行。你比孩子需要这个。"
            },
            ["gift_comfort_parent"] = new[]
            {
                "买了件新围裙，你做饭总溅到。……旧的我也洗了，备用。",
                "这个坐垫，喂奶时坐着用。……我查过了，对腰好。你舒服孩子才舒服。",
                "保温杯，你总忘记喝水。这个保温久，我提醒你。……你也需要被照顾。"
            },
            ["gift_surprise_parent"] = new[]
            {
                "……沙龙新出的蛋糕，我给你留了。孩子睡着后我们吃？就我们俩。",
                "这个给你。孩子问'爸爸买什么'，我说'给妈妈惊喜，因为妈妈最重要'。",
                "今天孩子第一次叫妈妈。……我买了个小蛋糕庆祝，你辛苦了。但你值得，不是因为你是妈妈，是因为你是你。"
            },
            ["gift_romantic_parent"] = new[]
            {
                "……我请了保姆今晚来看孩子。我带你出去，就我们俩。你还是我妻子，记得吗？",
                "买了瓶好酒，孩子睡了以后喝。……我想和你说话，不是关于孩子的，是关于你的。",
                "这个给你。别打开，等孩子睡了再看。……是内衣。我挑了很久。",
                "我写了首歌，关于你的。不是'孩子的妈妈'，是你。……要听吗？",
                "订了温泉旅馆，周末。罗宾答应看孩子。……我想和你单独待两天，像从前一样。"
            },

            // 给孩子买——温柔爸爸，但不拿你和孩子比较
            ["child_toy"] = new[]
            {
                "……给孩子买了个玩具。不是溺爱，是教育用的，开发智力。",
                "看到别的小孩有这个，觉得我们的孩子也该有。……就一个，不多买。",
                "孩子抓着我的手指不放，我就……买了。下不为例。"
            },
            ["child_book"] = new[]
            {
                "买了本图画书，晚上读给孩子听。……你也可以一起听，我喜欢你的声音。",
                "这个绘本，我小时候没有。想让孩子……有个不一样的童年。但你也重要，我买了两本，一本给你。",
                "书店老板推荐的，说适合这个月龄。我查了，确实好。……顺便给你带了本小说。"
            },
            ["child_clothes"] = new[]
            {
                "孩子长得太快了，买了件大一点的。……能穿两季，划算。",
                "这个颜色衬孩子肤色。……我对比了很久，不是随便选的。像你一样好看。",
                "买了双软底鞋，孩子学走路用。……我量了尺寸，正好。像你一样，小小的。"
            },
            ["child_food"] = new[]
            {
                "买了些婴儿辅食，有机的。……比普通的贵，但孩子吃的不能省。你的钱也不能省，我也给你买了零食。",
                "这个磨牙棒，孩子最近老咬东西。……我尝了，没味道，安全。不像你，你好吃多了。",
                "订购了新鲜果泥，每天现做太麻烦。……你休息，我来弄。你也吃点，别只喂孩子。"
            },
            ["child_education"] = new[]
            {
                "……给孩子报了早教班。我知道太早了，但想给他最好的。……也想给你最好的，所以我也报了烹饪班，我做晚饭。",
                "买了套积木，锻炼手眼协调。我玩了下，挺难的。……但没你难懂，你才是我最想研究明白的。",
                "这个音乐盒，能放古典乐。……我小时候没有，但我想给他。也想给你，你值得所有好东西。"
            },

            // 余额不足
            ["nofunds_newlywed"] = new[]
            {
                "……我想给你买点东西，但是看了眼账户。我们最近是不是……有点紧？",
                "我本来计划好要买个东西给你的。……再等等吧，等我想到怎么多赚点钱。",
                "电脑里有我想买的东西，也有我想给你的东西。但现在好像只能选一个。……我选你，但钱不够。"
            },
            ["nofunds_married"] = new[]
            {
                "……这个月花了比预想的多。不是乱花，就是……各种小事加起来。",
                "我想给你换双新鞋，但算了算。……再等等，下个月一定。",
                "我在记账了，真的。只是……记了还是不够。对不起，让你失望了。"
            },
            ["nofunds_parent"] = new[]
            {
                "……这个月紧了点。我少花点，你和孩子的不能省。……但我更想给你买，再等等。",
                "我想给孩子买那个玩具，但看了价格。……再存存，生日时买。但你的礼物我先存好了，不能等。",
                "钱不够同时给你和孩子买。……我先给你买。孩子可以等，你不行。"
            }
        };

        // ========== 收到礼物时的回应 ==========
        private readonly Dictionary<string, string[]> GiftResponses = new()
        {
            ["newlywed"] = new[]
            {
                "……喜欢吗？不用现在回答，我只是想让你知道，我有在注意你喜欢什么。",
                "你拿着吧。我研究了很久才决定买这个的。",
                "别抱我。……好吧，只能一下。"
            },
            ["married"] = new[]
            {
                "……实用吗？我特意选的，不是随便买的。",
                "你用得上就好。我观察过，你确实需要这个。",
                "不用谢。……好吧，谢谢也行。但你能亲我一下更好。"
            },
            ["parent"] = new[]
            {
                "……你值得这个。不是因为你是妈妈，是因为你是我的妻子。",
                "别省着不用，我买了就是给你用的。孩子也看着呢，让他知道妈妈最重要。",
                "你笑一下，比什么回报都好。……孩子睡了，我们能单独待会儿吗？"
            }
        };

        private readonly string[] ChildGiftResponses = new[]
        {
            "……给孩子买的。你别吃醋，你也有份，在桌上。你永远是第一位的。",
            "孩子喜欢吗？……我挑了很久，他/她应该喜欢吧。但你喜不喜欢更重要。",
            "我第一次买这种东西，不知道对不对。……你帮我看看？你比我有经验。什么都比我有经验。"
        };

        // ========== 邮件内容 ==========
        private readonly Dictionary<string, string[]> MailContents = new()
        {
            ["self_newlywed"] = new[]
            {
                "嘿。^我买了些电脑零件，花了点钱。别生气，我会想办法补上的。^^   -塞巴斯蒂安",
                "……我今天去镇上了。买了些东西，不是乱花。等你回家我解释。^^   -塞巴斯蒂安",
                "我买了张黑胶，想放给你听。晚上别睡太早。^^   -塞巴斯蒂安"
            },
            ["gift_newlywed"] = new[]
            {
                "给你。^我在店里看到这个，想到你。不是特意买的，就是……想到了。^^   -塞巴斯蒂安",
                "……我很少写这种东西。但想让你知道，我给你买了礼物。回家看。^^   -塞巴斯蒂安",
                "今天路过沙龙，蛋糕闻起来像你。给你带了一块。^^   -塞巴斯蒂安"
            },
            ["self_married"] = new[]
            {
                "我换了浴室的灯泡，亮的那个。你总说太暗。……我也买了新浴帘，旧的霉了。^^   -塞巴斯蒂安",
                "续费了开发工具，花了点钱。但晚上有更多时间陪你了，不加班。^^   -塞巴斯蒂安",
                "买了咖啡豆，早上磨。你闻闻，比沙龙的好。……我学着省钱了，但这个不能省。^^   -塞巴斯蒂安"
            },
            ["gift_married"] = new[]
            {
                "我给你买了东西。^别问为什么，问就是我想看你笑。^^   -塞巴斯蒂安",
                "……我在镇上看到这个，第一个想到你。不是孩子，不是别人，是你。^^   -塞巴斯蒂安",
                "今晚别做饭。我订了地方，带你出去。就我们俩。^^   -塞巴斯蒂安"
            },
            ["romantic_married"] = new[]
            {
                "我写了首歌。^歌词本在抽屉里，你先看看。等我练好了弹给你听。……是关于你的。^^   -塞巴斯蒂安",
                "周末我安排了惊喜。^罗宾答应帮忙看农场。我们要单独出去，像从前一样。^^   -塞巴斯蒂安",
                "……我想你了。^不是那种'你就在隔壁'的想，是那种……你看了就懂的想。晚上等我。^^   -塞巴斯蒂安"
            },
            ["self_parent"] = new[]
            {
                "我修了婴儿床，松了颗螺丝。……顺便买了本育儿书，但我也给你带了小说。别只照顾孩子。^^   -塞巴斯蒂安",
                "咖啡改自己冲了，省下的钱……给你买了东西。在桌上。^^   -塞巴斯蒂安",
                "孩子睡了。我买了瓶酒，等你。……我想和你说话，不是关于孩子的。^^   -塞巴斯蒂安"
            },
            ["gift_parent"] = new[]
            {
                "这个给你。^孩子问我'爸爸爱谁'，我说'爱妈妈，永远爱妈妈'。^^   -塞巴斯蒂安",
                "……你最近只给孩子买东西，给自己买点。我帮你买了。^^   -塞巴斯蒂安",
                "我请了保姆今晚来。我们出去，就我们俩。你还是我妻子，我最爱的人。^^   -塞巴斯蒂安"
            },
            ["romantic_parent"] = new[]
            {
                "我写了首歌。^关于你的，不是'孩子的妈妈'，是你。……孩子睡了，我弹给你听？^^   -塞巴斯蒂安",
                "周末我安排了温泉。^罗宾看孩子，就我们。我想和你单独待两天，像恋爱时一样。^^   -塞巴斯蒂安",
                "……你今晚真好看。^不是'辛苦的妈妈'好看，是'让我心动'好看。我买了酒，等你。^^   -塞巴斯蒂安"
            },
            ["child"] = new[]
            {
                "给孩子买了点东西。^……但我更想给你买。你的钱在枕头下，别让孩子看见。^^   -塞巴斯蒂安",
                "孩子有新玩具了。^但你也有，在柜子里。……你更重要，记得吗？^^   -塞巴斯蒂安",
                "我带孩子去镇上，顺便……给你买了这个。孩子问为什么，我说'因为妈妈最好看'。^^   -塞巴斯蒂安"
            },
            ["nofunds"] = new[]
            {
                "……我想给你买东西，但钱不够。^对不起。我在想办法，再等等我。^^   -塞巴斯蒂安",
                "这个月紧了点。^但我更想给你买，不是给孩子。孩子可以等，你不行。^^   -塞巴斯蒂安",
                "我在记账了，真的。^但记了还是不够。……别失望，我会努力的。为了你。^^   -塞巴斯蒂安"
            }
        };

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            random = new Random();
            
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Player.Warped += OnPlayerWarped;
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private string GetStage() => HasChildren ? "parent" : DaysMarried > 30 ? "married" : "newlywed";

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Config.Enabled) return;
            if (Game1.player.spouse != "Sebastian") return;
            if (DaysMarried <= 0) return;

            string stage = GetStage();
            double chance = stage switch
            {
                "newlywed" => Config.NewlywedChance,
                "married" => Config.MarriedChance,
                "parent" => Config.ParentChance,
                _ => 0.3
            };

            if (random.NextDouble() > chance) return;

            NPC seb = Game1.getCharacterFromName("Sebastian");
            if (seb == null) return;

            // 决定消费类型
            double giftChance = stage switch
            {
                "newlywed" => 0.35,
                "married" => 0.40, // 婚后更常给你买
                "parent" => 0.35,
                _ => 0.35
            };

            double romanticChance = stage == "married" ? 0.15 : stage == "parent" ? 0.20 : 0.0; // 婚后/有孩子后增加浪漫事件
            double roll = random.NextDouble();

            if (HasChildren && roll < 0.15) // 15%给孩子
            {
                ProcessChildSpending(seb, stage);
            }
            else if (roll < 0.15 + romanticChance) // 浪漫事件
            {
                ProcessRomanticSpending(seb, stage);
            }
            else if (roll < 0.15 + romanticChance + giftChance) // 普通礼物
            {
                ProcessGiftSpending(seb, stage, false);
            }
            else // 自己消费
            {
                ProcessSelfSpending(seb, stage);
            }
        }

        private void ProcessSelfSpending(NPC seb, string stage)
        {
            string[] categories = stage switch
            {
                "newlywed" => new[] { "gaming_newlywed", "coding_newlywed", "snacks_newlywed", "music_newlywed" },
                "married" => new[] { "gaming_married", "coding_married", "snacks_married", "music_married", "home_married" },
                "parent" => new[] { "gaming_parent", "coding_parent", "snacks_parent", "music_parent", "home_parent" },
                _ => new[] { "gaming_married" }
            };

            string category = categories[random.Next(categories.Length)];
            int amount = GetRandomAmount(category);

            if (Game1.player.Money < amount)
            {
                HandleNoFunds(seb, stage);
                return;
            }

            Game1.player.Money -= amount;
            string dialogue = GetRandomDialogue(category);
            
            RecordSpending(category, amount, false, false);
            
            if (Config.NotifyByDialogue)
                AddDialogue(seb, dialogue, amount, false, false, stage);
            
            if (Config.NotifyByMail)
                QueueMail("self_" + stage, amount, false);

            Monitor.Log($"[Sebastian] 自己消费: {amount}g ({category})", LogLevel.Debug);
        }

        private void ProcessGiftSpending(NPC seb, string stage, bool isRomantic)
        {
            string[] categories = stage switch
            {
                "newlywed" => new[] { "gift_crystal_newlywed", "gift_tech_newlywed", "gift_comfort_newlywed", "gift_surprise_newlywed" },
                "married" => isRomantic 
                    ? new[] { "gift_romantic_married" }
                    : new[] { "gift_crystal_married", "gift_tech_married", "gift_comfort_married", "gift_surprise_married", "gift_romantic_married" },
                "parent" => isRomantic
                    ? new[] { "gift_romantic_parent" }
                    : new[] { "gift_crystal_parent", "gift_tech_parent", "gift_comfort_parent", "gift_surprise_parent", "gift_romantic_parent" },
                _ => new[] { "gift_crystal_married" }
            };

            string category = categories[random.Next(categories.Length)];
            int amount = GetRandomAmount(category);

            if (Game1.player.Money < amount)
            {
                HandleNoFunds(seb, stage);
                return;
            }

            Game1.player.Money -= amount;
            
            string baseCategory = category.Replace("_newlywed", "").Replace("_married", "").Replace("_parent", "");
            Item gift = GenerateGiftItem(baseCategory);
            
            if (gift != null)
            {
                bool added = Game1.player.addItemToInventoryBool(gift);
                if (!added) Game1.currentLocation.debris.Add(new Debris(gift, Game1.player.Position));
                if (added) Game1.addHUDMessage(new HUDMessage($"塞巴斯蒂安送给你: {gift.DisplayName}", 2));
            }

            string dialogue = GetRandomDialogue(category);
            string response = GiftResponses[stage][random.Next(GiftResponses[stage].Length)];
            
            RecordSpending(category, amount, true, false);
            
            if (Config.NotifyByDialogue)
                AddDialogue(seb, dialogue + "#$" + response, amount, true, false, stage);
            
            if (Config.NotifyByMail)
                QueueMail(isRomantic ? "romantic_" + stage : "gift_" + stage, amount, true);

            Monitor.Log($"[Sebastian] 给你买礼物: {amount}g ({category})", LogLevel.Debug);
        }

        private void ProcessRomanticSpending(NPC seb, string stage)
        {
            ProcessGiftSpending(seb, stage, true);
        }

        private void ProcessChildSpending(NPC seb, string stage)
        {
            string[] categories = new[] { "child_toy", "child_book", "child_clothes", "child_food", "child_education" };
            string category = categories[random.Next(categories.Length)];
            int amount = GetRandomAmount(category);

            if (Game1.player.Money < amount)
            {
                HandleNoFunds(seb, stage);
                return;
            }

            Game1.player.Money -= amount;

            Item gift = GenerateGiftItem(category);
            if (gift != null)
            {
                bool added = Game1.player.addItemToInventoryBool(gift);
                if (!added) Game1.currentLocation.debris.Add(new Debris(gift, Game1.player.Position));
                if (added) Game1.addHUDMessage(new HUDMessage($"塞巴斯蒂安给孩子买了: {gift.DisplayName}", 2));
            }

            string dialogue = GetRandomDialogue(category);
            string response = ChildGiftResponses[random.Next(ChildGiftResponses.Length)];
            
            RecordSpending(category, amount, true, true);
            
            if (Config.NotifyByDialogue)
                AddDialogue(seb, dialogue + "#$" + response, amount, true, true, stage);
            
            if (Config.NotifyByMail)
                QueueMail("child", amount, true);

            Monitor.Log($"[Sebastian] 给孩子买: {amount}g ({category})", LogLevel.Debug);
        }

        private void HandleNoFunds(NPC seb, string stage)
        {
            string key = "nofunds_" + stage;
            string dialogue = GetRandomDialogue(key);
            
            if (Config.NotifyByDialogue)
                seb.setNewDialogue(dialogue + " $s", true, true);
            
            if (Config.NotifyByMail)
                QueueMail("nofunds", 0, false);
        }

        private Item GenerateGiftItem(string category)
        {
            if (!GiftItems.ContainsKey(category)) return null;
            int[] ids = GiftItems[category];
            int id = ids[random.Next(ids.Length)];
            Object obj = new Object(id, 1);
            if (category.Contains("crystal") && random.NextDouble() < 0.3)
                obj.Quality = 2;
            return obj;
        }

        private int GetRandomAmount(string category)
        {
            if (!SpendingRanges.ContainsKey(category)) return 100;
            return SpendingRanges[category][random.Next(SpendingRanges[category].Length)];
        }

        private string GetRandomDialogue(string category)
        {
            if (!Dialogues.ContainsKey(category)) return "……我今天花了点钱。";
            return Dialogues[category][random.Next(Dialogues[category].Length)];
        }

        private void AddDialogue(NPC seb, string text, int amount, bool isGift, bool isForChild, string stage)
        {
            if (seb == null) return;

            string fullText = text;
            if (Config.ShowAmountInDialogue)
            {
                string costInfo = isForChild ? $"(给孩子花了 {amount}g)" : isGift ? $"(给你买礼物花了 {amount}g)" : $"(自己花了 {amount}g)";
                fullText += $" $l#{costInfo}";
            }
            else fullText += " $l";

            seb.setNewDialogue(fullText, true, true);
            seb.modData["SebastianSpending_Today"] = "true";
        }

        // ========== 邮件系统 ==========
        private void QueueMail(string type, int amount, bool isGift)
        {
            var mailQueue = Helper.Data.ReadSaveData<MailQueue>("SebastianSpending_Mails") ?? new MailQueue();
            string mailId = $"SebastianSpending_{Game1.Date.TotalDays}_{random.Next(10000)}";
            
            string[] pool;
            if (type.StartsWith("romantic_")) pool = MailContents["romantic_" + type.Replace("romantic_", "")];
            else if (MailContents.ContainsKey(type)) pool = MailContents[type];
            else pool = MailContents["self_married"];
            
            string content = pool[random.Next(pool.Length)];
            
            // 添加物品奖励到邮件
            if (isGift && amount > 0)
            {
                // 邮件中附加少量金钱表示"省下的钱给你"
                content += $"%item money {Math.Min(amount / 10, 500)} %%";
            }
            
            mailQueue.PendingMails[mailId] = content;
            Helper.Data.WriteSaveData("SebastianSpending_Mails", mailQueue);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, string>();
                    var queue = Helper.Data.ReadSaveData<MailQueue>("SebastianSpending_Mails");
                    if (queue == null) return;
                    
                    foreach (var mail in queue.PendingMails)
                    {
                        if (!editor.Data.ContainsKey(mail.Key))
                            editor.Data[mail.Key] = mail.Value;
                    }
                });
            }
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            NPC seb = Game1.getCharacterFromName("Sebastian");
            if (seb != null && seb.modData.ContainsKey("SebastianSpending_Today"))
                seb.modData.Remove("SebastianSpending_Today");

            // 发送待处理邮件
            var queue = Helper.Data.ReadSaveData<MailQueue>("SebastianSpending_Mails");
            if (queue == null) return;

            foreach (var mail in queue.PendingMails)
            {
                if (!Game1.player.mailReceived.Contains(mail.Key))
                {
                    Game1.player.mailForTomorrow.Add(mail.Key);
                }
            }
            queue.PendingMails.Clear();
            Helper.Data.WriteSaveData("SebastianSpending_Mails", queue);
        }

        private void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;
            if (e.NewLocation.Name != "FarmHouse" && e.NewLocation.Name != "Cabin") return;
            
            NPC seb = Game1.getCharacterFromName("Sebastian");
            if (seb == null || seb.currentLocation != e.NewLocation) return;
            
            string key = $"SebastianSpending_{Game1.Date.TotalDays}";
            var record = Helper.Data.ReadSaveData<DayRecord>(key);
            if (record != null && record.Entries.Count > 0 && !seb.modData.ContainsKey("SebastianSpending_Today"))
            {
                var entry = record.Entries[record.Entries.Count - 1];
                string dialogue = GetRandomDialogue(entry.Category);
                
                if (entry.IsGift)
                {
                    string stage = GetStage();
                    if (entry.IsForChild)
                        dialogue += "#$" + ChildGiftResponses[random.Next(ChildGiftResponses.Length)];
                    else
                        dialogue += "#$" + GiftResponses[stage][random.Next(GiftResponses[stage].Length)];
                }
                
                AddDialogue(seb, dialogue, entry.Amount, entry.IsGift, entry.IsForChild, GetStage());
            }
        }

        private void RecordSpending(string category, int amount, bool isGift, bool isForChild)
        {
            string key = $"SebastianSpending_{Game1.Date.TotalDays}";
            var record = Helper.Data.ReadSaveData<DayRecord>(key) ?? new DayRecord();
            record.Entries.Add(new SpendingEntry 
            { 
                Category = category, 
                Amount = amount, 
                IsGift = isGift,
                IsForChild = isForChild,
                Time = Game1.timeOfDay 
            });
            Helper.Data.WriteSaveData(key, record);
        }
    }

    public class ModConfig
    {
        public bool Enabled { get; set; } = true;
        public double NewlywedChance { get; set; } = 0.40;
        public double MarriedChance { get; set; } = 0.40; // 婚后概率提高，更常给你买
        public double ParentChance { get; set; } = 0.45;
        public bool NotifyByDialogue { get; set; } = true;
        public bool NotifyByMail { get; set; } = true; // 新增：邮件通知
        public bool ShowAmountInDialogue { get; set; } = false;
        public int MaxDailySpending { get; set; } = 2000;
    }

    public class DayRecord
    {
        public List<SpendingEntry> Entries { get; set; } = new();
    }

    public class SpendingEntry
    {
        public string Category { get; set; }
        public int Amount { get; set; }
        public bool IsGift { get; set; }
        public bool IsForChild { get; set; }
        public int Time { get; set; }
    }

    public class MailQueue
    {
        public Dictionary<string, string> PendingMails { get; set; } = new();
    }
}

