using budget.Services;

namespace budget.Shared
{
    public class WriterConfigurator
    {
        protected static IWriter? writer_;
        private WriterConfigurator() { }
        public static JSONWriter GetJsonWriter(string fileName)
        {
            if (writer_ is null)
                writer_ = new JSONWriter(fileName);

            return (JSONWriter)writer_;
        }
        public static ConsoleWriter GetConsoleWriter()
        {
            if (writer_ is null)
                writer_ = new ConsoleWriter();
            return (ConsoleWriter)writer_;
        }
        public static IWriter? GetWriter()
        {
            return writer_;
        }
    }
}
