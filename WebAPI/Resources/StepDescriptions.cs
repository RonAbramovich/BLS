namespace BLS.WebAPI.Resources
{
    /// <summary>
    /// Localization strings for BLS signature steps
    /// </summary>
    public static class StepDescriptions
    {
        public static class Hebrew
        {
            public const string Step1 = "הגדרת שדה ראשוני ועקום אליפטי";
            public const string Step2 = "חישוב סדר החבורה של העקום";
            public const string Step3 = "חישוב P = H(m) - גיבוב ההודעה לנקודה על העקומה";
            public const string Step4 = "חישוב מעלת השיכון k";
            public const string Step5 = "מציאת פולינום בלתי פריק מעל F_q ממעלה k";
            public const string Step6 = "יצירת שדה הרחבה F_q^k ועקום מעל השדה המורחב";
            public const string Step7 = "מציאת נקודה Q בתת-קבוצת r-טורסיה של E(F_q^k)";
            public const string Step8 = "חישוב חתימת אליס: e_r(a*H(m), Q)";
            public const string Step9 = "חישוב אימות בוב: e_r(H(m), a*Q)";
        }

        public static class English
        {
            public const string Step1 = "Define prime field and elliptic curve";
            public const string Step2 = "Calculate curve group order";
            public const string Step3 = "Calculate P = H(m) - hash message to curve point";
            public const string Step4 = "Calculate embedding degree k";
            public const string Step5 = "Find irreducible polynomial over F_q of degree k";
            public const string Step6 = "Create extension field F_q^k and curve over extension";
            public const string Step7 = "Find point Q in r-torsion subgroup of E(F_q^k)";
            public const string Step8 = "Calculate Alice signature: e_r(a*H(m), Q)";
            public const string Step9 = "Calculate Bob verification: e_r(H(m), a*Q)";
        }

        public static string GetDescription(int stepNumber, string language)
        {
            var isHebrew = language.ToLower() == "he";
            
            return stepNumber switch
            {
                1 => isHebrew ? Hebrew.Step1 : English.Step1,
                2 => isHebrew ? Hebrew.Step2 : English.Step2,
                3 => isHebrew ? Hebrew.Step3 : English.Step3,
                4 => isHebrew ? Hebrew.Step4 : English.Step4,
                5 => isHebrew ? Hebrew.Step5 : English.Step5,
                6 => isHebrew ? Hebrew.Step6 : English.Step6,
                7 => isHebrew ? Hebrew.Step7 : English.Step7,
                8 => isHebrew ? Hebrew.Step8 : English.Step8,
                9 => isHebrew ? Hebrew.Step9 : English.Step9,
                _ => $"Step {stepNumber}"
            };
        }
    }
}
