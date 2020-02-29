using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DeliveryManagement
{

    //Class Config retrieves information about Dostavista API connection from the Config Entity
    class Config
    {
        public string apiKey;
        public string apiURL;

        public (string, string) GetConfig(IOrganizationService service)
        {
            //Fetch API configuration from Dostavista Config Entity
            string configFetchXml = $@"
            <fetch>
                <entity name='odteam_dostavistaconfig'>
                    <attribute name='odteam_apikey' />
                    <attribute name='odteam_apiurl' />
                </entity>
            </fetch>";

            EntityCollection dConfig = service.RetrieveMultiple(new FetchExpression(configFetchXml));
            return (apiKey = dConfig.Entities[0].Attributes["odteam_apikey"].ToString(), apiURL = dConfig.Entities[0].Attributes["odteam_apiurl"].ToString());  //Need to check if there are more than 1 Dostavista Config Entity - not yet implemented
                                                    
        }
    }
}
