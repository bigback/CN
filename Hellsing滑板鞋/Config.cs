using System.Collections.Generic;

using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Kalista
{
    public class Config
    {
        private static bool initialized = false;
        private const string MENU_TITLE = "[Hellsing滑板鞋] " + Program.CHAMP_NAME ;

        private static MenuWrapper _menu;

        private static Dictionary<string, MenuWrapper.BoolLink> _boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        private static Dictionary<string, MenuWrapper.CircleLink> _circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        private static Dictionary<string, MenuWrapper.KeyBindLink> _keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        private static Dictionary<string, MenuWrapper.SliderLink> _sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        public static MenuWrapper Menu { get { return _menu; } }

        public static Dictionary<string, MenuWrapper.BoolLink> BoolLinks { get { return _boolLinks; } }
        public static Dictionary<string, MenuWrapper.CircleLink> CircleLinks { get { return _circleLinks; } }
        public static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks { get { return _keyLinks; } }
        public static Dictionary<string, MenuWrapper.SliderLink> SliderLinks { get { return _sliderLinks; } }

        private static void ProcessLink(string key, object value)
        {
            if (value is MenuWrapper.BoolLink)
                _boolLinks.Add(key, value as MenuWrapper.BoolLink);
            else if (value is MenuWrapper.CircleLink)
                _circleLinks.Add(key, value as MenuWrapper.CircleLink);
            else if (value is MenuWrapper.KeyBindLink)
                _keyLinks.Add(key, value as MenuWrapper.KeyBindLink);
            else if (value is MenuWrapper.SliderLink)
                _sliderLinks.Add(key, value as MenuWrapper.SliderLink);
        }

        static Config()
        {
            // Create menu
            _menu = new MenuWrapper(MENU_TITLE);

            // Combo
            var subMenu = _menu.MainMenu.AddSubMenu("连招");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("使用 Q"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("使用 E"));
            ProcessLink("comboNumE", subMenu.AddLinkedSlider("叠E层数", 5, 1, 20));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("使用物品"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("使用点燃"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("连招按键", 32, KeyBindType.Press));

            // Harass
            subMenu = _menu.MainMenu.AddSubMenu("消耗");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("使用 Q"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("保留蓝量 (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("消耗按键", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("清线");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("使用 Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("自动Q最少击杀小兵数", 3, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("使用 E"));
            ProcessLink("waveNumE", subMenu.AddLinkedSlider("自动E最少击杀小兵数", 2, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("保留蓝量 (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("清线按键", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("清野");
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("使用 E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("清野按键", 'V', KeyBindType.Press));

            // Flee
            subMenu = _menu.MainMenu.AddSubMenu("逃跑");
            ProcessLink("fleeWalljump", subMenu.AddLinkedBool("尝试跳过墙"));
            ProcessLink("fleeAA", subMenu.AddLinkedBool("智能平A"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("逃跑按键", 'T', KeyBindType.Press));

            // Misc
            subMenu = _menu.MainMenu.AddSubMenu("其他");
            ProcessLink("miscKillstealE", subMenu.AddLinkedBool("E抢人头"));
            ProcessLink("miscBigE", subMenu.AddLinkedBool("总是自动E抢大型野怪"));
            ProcessLink("miscUseR", subMenu.AddLinkedBool("使用R救队友"));
            ProcessLink("miscAutoE", subMenu.AddLinkedBool("自动E击杀补不到的兵"));
            ProcessLink("miscAutoEchamp", subMenu.AddLinkedBool("当敌人身上有1根以上矛时自动E可击杀的单位"));

            // Spell settings
            subMenu = _menu.MainMenu.AddSubMenu("技能设置");
            ProcessLink("spellReductionE", subMenu.AddLinkedSlider("E伤害计算减少（避免出现E不死）", 20));

            // Items
            subMenu = _menu.MainMenu.AddSubMenu("物品使用");
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("使用比尔吉沃特弯刀"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("使用破败"));
            ProcessLink("itemsYoumuu", subMenu.AddLinkedBool("使用幽梦"));

            // Drawings
            subMenu = _menu.MainMenu.AddSubMenu("显示");
            ProcessLink("drawDamageE", subMenu.AddLinkedCircle("在血条上显示E伤害", true, Color.FromArgb(150, Color.Green), 0));
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q 范围", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeW", subMenu.AddLinkedCircle("W 范围", true, Color.FromArgb(150, Color.MediumPurple), SpellManager.W.Range));
            ProcessLink("drawRangeEsmall", subMenu.AddLinkedCircle("E 范围 ", false, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range - 200));
            ProcessLink("drawRangeEactual", subMenu.AddLinkedCircle("E 范围 (真实)", true, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R 范围", false, Color.FromArgb(150, Color.Red), SpellManager.R.Range));
        }
    }
}
