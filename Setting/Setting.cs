using System;
using UnityEngine;

namespace ModSettingTest1 {
    public static class Setting {
        public static string Dropdown1 { get; private set; }
        public static string Dropdown2 { get; private set; }
        public static bool Toggle1 { get; private set; }
        public static bool Toggle2 { get; private set; }
        public static float Slider1 { get; private set; }
        public static float Slider2 { get; private set; }
        public static string Input1 { get; private set; }
        public static string Input2 { get; private set; }
        public static KeyCode Keybinding1 { get; private set; }
        public static KeyCode Keybinding2 { get; private set; }
        public static event Action<float> OnSlider1ValueChanged;
        public static void SetDropdown1(string value) => Dropdown1 = value;
        public static void SetDropdown2(string value) => Dropdown2 = value;
        public static void SetToggle1(bool value) => Toggle1 = value;
        public static void SetToggle2(bool value) => Toggle2 = value;
        public static void SetSlider1(float value) {
            Slider1 = value;
            OnSlider1ValueChanged?.Invoke(value);
        }

        public static void SetSlider2(float value) => Slider2 = value;
        public static void SetInput1(string value) => Input1 = value;
        public static void SetInput2(string value) => Input2 = value;
        public static void SetKeybinding1(KeyCode value) => Keybinding1 = value;
        public static void SetKeybinding2(KeyCode value) => Keybinding2 = value;
        
        public static void Clear() {
            OnSlider1ValueChanged = null;
        }

        public static void Init() {
            if (ModSettingAPI.HasConfig()) {
                Dropdown1 = ModSettingAPI.GetSavedValue("D1", out string d1) ? d1 : "选项2";
                Dropdown2 = ModSettingAPI.GetSavedValue("D2", out string d2) ? d2 : "选项9";
                Toggle1 = ModSettingAPI.GetSavedValue("T1", out bool t1) ? t1 : false;
                Toggle2 = ModSettingAPI.GetSavedValue("T2", out bool t2) ? t2 : true;
                Slider1 = ModSettingAPI.GetSavedValue("S1", out float s1) ? s1 : 0;
                Slider2 = ModSettingAPI.GetSavedValue("S2", out float s2) ? s2 : 0;
                Input1 = ModSettingAPI.GetSavedValue("I1", out string i1) ? i1 : "输入框1";
                Input2 = ModSettingAPI.GetSavedValue("I2", out string i2) ? i2 : "输入框2";
                Keybinding1 = ModSettingAPI.GetSavedValue("K1", out KeyCode k1) ? k1 : KeyCode.N;
                Keybinding2 = ModSettingAPI.GetSavedValue("K2", out KeyCode k2) ? k2 : KeyCode.None;
            } else {
                //设置默认值
                Dropdown1 = "选项2";
                Dropdown2 = "选项9";
                Toggle1 = false;
                Toggle2 = true;
                Keybinding1 = KeyCode.N;
                Keybinding2 = KeyCode.None;
            }
        }
    }
}