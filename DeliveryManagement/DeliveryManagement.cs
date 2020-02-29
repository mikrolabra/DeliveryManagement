using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Newtonsoft.Json;

namespace DeliveryManagement
{
    public class CreateDelivery : CodeActivity
    {
        string recipientContactPhone;
        string recipientContactPerson;
        string orderProduct;
        string startLocation;
        string destinationLocation;
        string senderContactPhone;
              
        //Input arguments for the workflow
        [Input("Contact_Phone")]
        public InArgument<string> ContactPhone { get; set; }

        [Input("Contact Name")]
        public InArgument<string> ContactName { get; set; }

        [Input("Start")]
        public InArgument<string> StartLocation { get; set; }

        [Input("Destination")]
        public InArgument<string> DestinationLocation { get; set; }

        [Input("My Contact Phone")]
        public InArgument<string> MyContactPhone { get; set; }

        //Output arguments for the workflow
        [Output("Dostavista Order ID")]
        public OutArgument<string> dostavistaOrderId { get; set; }

        [Output("Delivery Payment Amount")]
        public OutArgument<string> deliveryPaymentAmount { get; set; }

        [Output("Transport Type")]
        public OutArgument<string> deliveryTransportType { get; set; }

        [Output("Delivery Status")]
        public OutArgument<string> deliveryStatus { get; set; }

        [Output("Delivery Creation Date")]
        public OutArgument<DateTime> deliveryCreationDate { get; set; }

       

        protected override void Execute(CodeActivityContext context)
        {
            Dictionary<int, string> statusDictionary = new Dictionary<int, string>(2);
            statusDictionary.Add(1, "Created");
            statusDictionary.Add(2, "Error");
            var workflowContext = context.GetExtension<IWorkflowContext>();                    //Get the workflow execution context
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();          //Create an organization service
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            //Get the workflow's input parameters
            recipientContactPhone = this.ContactPhone.Get<string>(context);
            recipientContactPerson = this.ContactName.Get<string>(context);
            startLocation = this.StartLocation.Get<string>(context);
            destinationLocation = this.DestinationLocation.Get<string>(context);
            senderContactPhone = this.MyContactPhone.Get<string>(context);
            Guid recordId = workflowContext.PrimaryEntityId;                                   //Get Order Entity Guid

            //Get info about Order's products (Salesorderdetail Entity) with FetchXML request and write into entity collection
            var fetchXml = $@"
            <fetch>
                <entity name='salesorderdetail'>
                    <attribute name='salesorderdetailname' />
                    <attribute name='quantity' />
                        <filter type='and'>
                            <condition attribute='salesorderid' operator='eq' value='{recordId.ToString()}'/>
                        </filter>
                </entity>
            </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            foreach (var c in result.Entities)
            {
               orderProduct = orderProduct + c.Attributes["salesorderdetailname"].ToString() + ": " + c.Attributes["quantity"].ToString() + "pcs ";
            }

            //Get HTTP responce from Dostavista API and save into JSON
            var postResponse = PostRequestAsync(senderContactPhone, startLocation, destinationLocation, recipientContactPhone, recipientContactPerson, orderProduct, service).Result;
            string json = postResponse.ToString();
            //Deserialize JSON object and update workflow's output arguments
            //JSON schema is here https://test.dostavista.ru/business-api/doc#orders 
            RootObject dOrder = JsonConvert.DeserializeObject<RootObject>(json);
            bool dCreationStatus = dOrder.is_successful;
            string dStatus;
            if (dOrder.is_successful)
            {
                dostavistaOrderId.Set(context, dOrder.order.order_id.ToString());
                deliveryPaymentAmount.Set(context, dOrder.order.payment_amount);
                dStatus = statusDictionary[1];                                              
            }
            else
            {
                dStatus = statusDictionary[2];
            }
            deliveryStatus.Set(context, dStatus);                                       //Set Dostavista Delivery Status (Delivery Entity)
            deliveryCreationDate.Set(context, dOrder.order.created_datetime);           //Set Dostavista Delivery Creation Date (Delivery Entity)
        }

        public static async Task<string> PostRequestAsync(string myContactPhone, string startLocation, string destinationLocation, string contactPhone, string contactPerson, string orderProduct, IOrganizationService service)
        {
            //Method for creation a new POST request to Dostavista API
            string apiKey;
            string apiURL;
            
            //Get API configuration
            Config dConfig = new Config();
            var apiSettings = dConfig.GetConfig(service);
            apiKey = apiSettings.Item1;
            apiURL = apiSettings.Item2;

            WebRequest request = WebRequest.Create($"{apiURL}/create-order");
            request.Method = "POST";
            request.Headers.Add("X-DV-Auth-Token", apiKey);
            
            //Create new Dostavista Delivery Order and serialize it
            ContactPerson sender = new ContactPerson
            {
                phone = myContactPhone
            };
            ContactPerson recipient = new ContactPerson
            {
                name = contactPerson,
                phone = contactPhone
            };
            Point startPoint = new Point
            {
                address = startLocation,
                contact_person = sender
            };
            Point destinationPoint = new Point
            {
                address = destinationLocation,
                contact_person = recipient
            };
            Order dOrder = new Order();
            dOrder.matter = orderProduct;
            dOrder.points = new List<Point>();
            dOrder.points.Add(startPoint);
            dOrder.points.Add(destinationPoint);
            string data = JsonConvert.SerializeObject(dOrder, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);            //Encode to UTF8 array
            request.ContentType = "application/x-www-form-urlencoded";              //Set content type of the request
            request.ContentLength = byteArray.Length;                               //Set ContentLength header
            using (Stream dataStream = request.GetRequestStream())                  //Send POST request
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            
            //Get and return HTTP response
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseString.ToString();

        }

    }

    public class CancelDelivery : CodeActivity
    {
        string dDeliveryId;
        string apiKey;
        string apiURL;

        [RequiredArgument]
        [Input("Dostavista Delivery ID")]
        public InArgument<string> dostavistaDeliveryId { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var workflowContext = context.GetExtension<IWorkflowContext>();                                    //Get the workflow execution context
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();                          //Create an organization service
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            dDeliveryId = dostavistaDeliveryId.Get<string>(context);                                           //Get Delivery Entity Guid

            //Get API configuration
            Config dConfig = new Config();
            var apiSettings = dConfig.GetConfig(service);
            apiKey = apiSettings.Item1;
            apiURL = apiSettings.Item2;

            WebRequest request = WebRequest.Create($"{apiURL}/cancel-order");
            request.Method = "POST";
            request.Headers.Add("X-DV-Auth-Token", apiKey);
            string data = "{\"order_id\":" + dDeliveryId + "}";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);        //Encode to UTF8 array
            request.ContentType = "application/x-www-form-urlencoded";          //Set content type of the request
            request.ContentLength = byteArray.Length;                           //Set ContentLength header
            using (Stream dataStream = request.GetRequestStream())              //Send POST request
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            //Get HTTP response           
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

        }
    }

   
}

