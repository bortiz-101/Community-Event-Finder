namespace Community_Event_Finder.Models

{

    public class IngestionException : Exception

    {

        public string ProviderName { get; }



        public IngestionException(string providerName, string message)

            : base(message)

        {

            ProviderName = providerName;

        }



        public IngestionException(string providerName, string message, Exception innerException)

            : base(message, innerException)

        {

            ProviderName = providerName;

        }

    }

}

