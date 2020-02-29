using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DeliveryManagement
{
    //Order class object stores information about Dostavista Delivery Order
    public class Order
    {       
            public List<Point> points { get; set; }
            public string matter { get; set; }
            public int order_id { get; set; }
            public string order_name { get; set; }
            public int vehicle_type_id { get; set; }
            public DateTime created_datetime { get; set; }
            public object finish_datetime { get; set; }
            public string status { get; set; }
            public int total_weight_kg { get; set; }
            public bool is_client_notification_enabled { get; set; }
            public bool is_contact_person_notification_enabled { get; set; }
            public int loaders_count { get; set; }
            public object backpayment_details { get; set; }
            public string payment_amount { get; set; }
            public string delivery_fee_amount { get; set; }
            public string weight_fee_amount { get; set; }
            public string insurance_amount { get; set; }
            public string insurance_fee_amount { get; set; }
            public string loading_fee_amount { get; set; }
            public string money_transfer_fee_amount { get; set; }
            public string suburban_delivery_fee_amount { get; set; }
            public string overnight_fee_amount { get; set; }
            public string discount_amount { get; set; }
            public string backpayment_amount { get; set; }
            public string cod_fee_amount { get; set; }
            public object backpayment_photo_url { get; set; }
            public object itinerary_document_url { get; set; }
            public object waybill_document_url { get; set; }
            public object courier { get; set; }
            public bool is_motobox_required { get; set; }
        
    }

    //Point class object stores information about Dostavista Delivery Order address
    public class Point
    {
        public string address { get; set; }
        public ContactPerson contact_person { get; set; }
        public int point_id { get; set; }
        public object client_order_id { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public DateTime required_start_datetime { get; set; }
        public DateTime required_finish_datetime { get; set; }
        public object arrival_start_datetime { get; set; }
        public object arrival_finish_datetime { get; set; }
        public object courier_visit_datetime { get; set; }
        public string taking_amount { get; set; }
        public string buyout_amount { get; set; }
        public object note { get; set; }
        public List<object> packages { get; set; }
        public bool is_cod_cash_voucher_required { get; set; }
        public bool is_order_payment_here { get; set; }
        public object building_number { get; set; }
        public object entrance_number { get; set; }
        public object intercom_code { get; set; }
        public object floor_number { get; set; }
        public object apartment_number { get; set; }
        public object invisible_mile_navigation_instructions { get; set; }
    }

    //Contact Person class object stores information about Dostavista Delivery Order contact person
    public class ContactPerson
    {
        public object name { get; set; }
        public string phone { get; set; }
    }

    //Root Object class object contains information about Dostavista Delivery Order creation status
    public class RootObject
    {
        public bool is_successful { get; set; }
        public Order order { get; set; }
    }
}
