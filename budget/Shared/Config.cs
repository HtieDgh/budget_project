namespace budget.Shared
{
    /// <summary>
    /// Пердставление конфигов Expense
    /// </summary>
    public class ExpenseConfig
    {
        public required string Path { get; set; }
        public required string[] HeaderShema { get; set; }
        public required string[] DateFormats { get; set; }
        public required int MaxPossibleStrategies { get; set; }
    }
    /// <summary>
    /// Пердставление конфигов: Options
    /// </summary>
    public class OptionsConfig
    {
        public required string[] DateFormats { get; set; }
    }
    /// <summary>
    /// Пердставление конфигов: OCRReader
    /// </summary>
    public class OCRReaderConfig
    {
        public required string DefaultTessdataDirectory { get; set; }
    }
    /// <summary>
    /// Пердставление конфигов: VoiceReader
    /// </summary>
    public class VoiceReaderConfig
    {
        public required string DefaultVoiceModelDirectory { get; set; }
        public required string CurrentLanguageModel { get; set; }
    }

    public class Config
    {
        static Config? instance_;
        private OCRReaderConfig? ocrConfig_;
        private OptionsConfig? optionsConfig_;
        private ExpenseConfig? expenseConfig_;
        private VoiceReaderConfig? voiceReaderConfig_;

        Config() { }
        public static Config i() {
            if (instance_ is null) instance_ = new Config(); 
            return instance_;
        }
        public ExpenseConfig ExpenseConfig
        {
            get
            {
                if (expenseConfig_ is null) throw new Exception("Please, do Config.i().SetConfig before acces to ExpenseConfig");
                else return expenseConfig_;
            }
            private set => expenseConfig_ = value;
        }
        public OptionsConfig OptionsConfig
        {
            get
            {
                if (optionsConfig_ is null) throw new Exception("Please, do Config.i().SetConfig before acces to OptionsConfig");
                else return optionsConfig_;
            }
            private set => optionsConfig_ = value;
        }
        public OCRReaderConfig OcrConfig
        {
            get
            {
                if (ocrConfig_ is null) throw new Exception("Please, do Config.i().SetConfig before acces to OcrConfig");
                else return ocrConfig_;
            }
            private set => ocrConfig_ = value;
        }
        public VoiceReaderConfig VoiceReaderConfig
        {
            get
            {
                if (voiceReaderConfig_ is null) throw new Exception("Please, do Config.i().SetConfig before acces to VoiceReaderConfig");
                else return voiceReaderConfig_;
            }
            private set => voiceReaderConfig_ = value;
        }
        public Config SetConfig(ExpenseConfig expenseConfig, OptionsConfig optionsConfig, OCRReaderConfig ocrConfig, VoiceReaderConfig voiceConfig)
        {
            ExpenseConfig = expenseConfig;
            OptionsConfig = optionsConfig;
            OcrConfig = ocrConfig;
            VoiceReaderConfig = voiceConfig;
            return this;
        }

    }
}
