﻿using Stripe;
using System;
using System.Web.Configuration;
using Travelopedia.Models;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using Travelopedia_API.Models;
using System.Net.Http;
using Microsoft.AspNet.Identity;

namespace Travelopedia
{
    public partial class Home : System.Web.UI.Page
    {
        public string stripePublishableKey = WebConfigurationManager.AppSettings["StripePublishableKey"];
        public string amount = "0.0";
        public double amt = 0;
        public string[] sessionValues;
        public string DataType = "";

        public PaymentDetails paymentDetails;
        protected void Page_Load(object sender, EventArgs e)
        {
                paymentDetails = new PaymentDetails();
            if (User.Identity.IsAuthenticated)
            {
                if (Session["Data"].ToString() != "")
                {
                    hiddenFieldLogin.Value = "logout";
                    // string username = Request.Cookies["TimedCookie"]["User"].ToString();
                    Session.Timeout = 10;

                    int timeout = Session.Timeout * 1000 * 60;

                    DateTime sessionStart = (DateTime)Session["Timer"];

                    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    TimeSpan diff = sessionStart.ToUniversalTime() - origin;

                    int timerStarted = (int)Math.Floor(diff.TotalSeconds);

                    origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    diff = DateTime.Now.ToUniversalTime() - origin;
                    int timerNow = (int)Math.Floor(diff.TotalSeconds);

                    timeout = ((timeout / 1000) - (timerNow - timerStarted)) * 1000;

                    JToken token = JObject.Parse(Session["Data"].ToString());
                    DataType = Session["DataType"].ToString();

                    ClientScript.RegisterStartupScript(this.GetType(), "SessionAlert", "SessionExpireAlert(" + timeout + ");", true);

                    if (DataType == "flightround")
                    {
                        FlightDetailsRound.Visible = true;
                        CarDetails.Visible = false;

                        FlightDetails fd = new FlightDetails();
                        PaymentDetails pd = new PaymentDetails();

                        DateTime sDate = DateTime.Parse(token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("departureTime").ToString());

                        depttime1.Text = sDate.ToShortTimeString();
                        deptdate1.Text = sDate.ToShortDateString();
                        dept1.Text = token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("origin").ToString();

                        DateTime rDate = DateTime.Parse(token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("arrivalTime").ToString());
                        
                        arrivetime1.Text = rDate.ToShortTimeString();
                        arrivedate1.Text = rDate.ToShortDateString();
                        arrive1.Text = token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("destination").ToString();
                        duration1.Text = token.SelectToken("slice")[0].SelectToken("duration").ToString();

                        DateTime sDate2 = DateTime.Parse(token.SelectToken("slice")[1].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("departureTime").ToString());

                        depttime2.Text = sDate2.ToShortTimeString();
                        deptdate2.Text = sDate2.ToShortDateString();
                        dept2.Text = token.SelectToken("slice")[1].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("origin").ToString();

                        DateTime rDate2 = DateTime.Parse(token.SelectToken("slice")[1].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("arrivalTime").ToString());

                        returnflightlocation.InnerText = Session["Location"].ToString();
                        returntotalamounttext.InnerText = token.SelectToken("pricing")[0].SelectToken("saleFareTotal").ToString();
                        returntotaltax.InnerText = token.SelectToken("pricing")[0].SelectToken("saleTaxTotal").ToString();

                        arrivetime2.Text = rDate2.ToShortTimeString();
                        arrivedate2.Text = rDate2.ToShortDateString();
                        arrive2.Text = token.SelectToken("slice")[1].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("destination").ToString();
                        duration2.Text = token.SelectToken("slice")[1].SelectToken("duration").ToString();


                        flightprice.Text = token.SelectToken("saleTotal").ToString();

                        string amt = token.SelectToken("saleTotal").ToString().Substring(3, token.SelectToken("saleTotal").ToString().Length - 3);
                        amount = (Convert.ToDouble(amt) * 100).ToString();

                        if (Request.Form["stripeToken"] != null)
                        {
                            var customers = new StripeCustomerService();
                            var charges = new StripeChargeService();

                            var customer = customers.Create(new StripeCustomerCreateOptions
                            {
                                Email = Request.Form["stripeEmail"],
                                SourceToken = Request.Form["stripeToken"]
                            });

                            var charge = charges.Create(new StripeChargeCreateOptions
                            {
                                Amount = Convert.ToInt32(Convert.ToDouble(amount) * 100),
                                Description = "Sample Charge",
                                Currency = "usd",
                                CustomerId = customer.Id,
                                ReceiptEmail = customer.Email,
                                SourceTokenOrExistingSourceId = customer.DefaultSourceId
                            });

                            StripeConfiguration.SetApiKey(WebConfigurationManager.AppSettings["StripeSecretKey"]);

                            var chargeService = new StripeChargeService();
                            StripeCharge charges2 = chargeService.Get(charge.Id);

                            if (charges2.Captured == true)
                            {
                                pd.UserID = User.Identity.GetUserId();
                                pd.PaymentAmount = Convert.ToDouble(amount) * 100;
                                pd.PaymentDate = DateTime.Now;
                                pd.StripePaymentID = charge.Id;

                                fd.UserID = User.Identity.GetUserId();
                                fd.paymentdetails = pd;
                                fd.FlightType = "Round";
                                fd.Source = depto.Text;
                                fd.Destination = arriveo.Text;
                                fd.ArrivalDate1 = rDate.ToShortDateString();
                                fd.ArrivalTime1 = rDate.ToShortTimeString();
                                fd.DepartureDate1 = sDate.ToShortDateString();
                                fd.DepartureTime1 = sDate.ToShortTimeString();
                                fd.ArrivalDate2 = rDate2.ToShortDateString();
                                fd.ArrivalTime2 = rDate2.ToShortTimeString();
                                fd.DepartureDate2 = sDate2.ToShortDateString();
                                fd.DepartureTime2 = sDate2.ToShortTimeString();

                                using (var client = new HttpClient())
                                {
                                    string responseString = "";
                                    client.BaseAddress = new Uri("http://localhost/Travelopedia-api/api/");
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                    var response = client.PostAsJsonAsync("flights/ConfirmFlight/", fd).Result;

                                    if (response.IsSuccessStatusCode)
                                    {
                                        responseString = response.Content.ReadAsStringAsync().Result;
                                    }

                                    Session["Payment"] = charges2;
                                    Session["token"] = responseString;
                                    Response.Redirect("SuccessPayment.aspx");
                                }
                            }
                            else
                            {
                                Session["Payment"] = charges2;
                                Response.Redirect("ErrorPayment.aspx");
                            }


                        }
                    }
                    else if (DataType == "flightone")
                    {
                        FlightDetailsOne.Visible = true;
                        CarDetails.Visible = false;

                        FlightDetails fd = new FlightDetails();
                        PaymentDetails pd = new PaymentDetails();



                        DateTime oDate = DateTime.Parse(token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("departureTime").ToString());

                        depttimeo.Text = oDate.ToShortTimeString();
                        deptdateo.Text = oDate.ToShortDateString();
                        depto.Text = token.SelectToken("slice")[0].SelectToken("segment")[0].SelectToken("leg")[0].SelectToken("origin").ToString();

                       DateTime dDate = DateTime.Parse(token.SelectToken("slice")[0].SelectToken("segment")[1].SelectToken("leg")[0].SelectToken("arrivalTime").ToString());

                        flightlocation.InnerText = Session["Location"].ToString();

                        arrivetimeo.Text = dDate.ToShortTimeString();
                        arrivedateo.Text = dDate.ToShortDateString();
                        arriveo.Text = token.SelectToken("slice")[0].SelectToken("segment")[1].SelectToken("leg")[0].SelectToken("destination").ToString();

                        totalamounttext.InnerText = token.SelectToken("pricing")[0].SelectToken("saleFareTotal").ToString();
                        totaltax.InnerText = token.SelectToken("pricing")[0].SelectToken("saleTaxTotal").ToString();

                        durationo.Text = token.SelectToken("slice")[0].SelectToken("duration").ToString();
                        flightpriceo.Text = token.SelectToken("saleTotal").ToString();
                        string amt = token.SelectToken("saleTotal").ToString().Substring(3, token.SelectToken("saleTotal").ToString().Length - 3);
                        amount = (Convert.ToDouble(amt) * 100).ToString();


                        if (Request.Form["stripeToken"] != null)
                        {
                            var customers = new StripeCustomerService();
                            var charges = new StripeChargeService();

                            var customer = customers.Create(new StripeCustomerCreateOptions
                            {
                                Email = Request.Form["stripeEmail"],
                                SourceToken = Request.Form["stripeToken"]
                            });

                            var charge = charges.Create(new StripeChargeCreateOptions
                            {
                                Amount = Convert.ToInt32(Convert.ToDouble(amount) * 100),
                                Description = "Sample Charge",
                                Currency = "usd",
                                CustomerId = customer.Id,
                                ReceiptEmail = customer.Email,
                                SourceTokenOrExistingSourceId = customer.DefaultSourceId
                            });

                            StripeConfiguration.SetApiKey(WebConfigurationManager.AppSettings["StripeSecretKey"]);

                            var chargeService = new StripeChargeService();
                            StripeCharge charges2 = chargeService.Get(charge.Id);

                            if (charges2.Captured == true)
                            {
                                pd.UserID = User.Identity.GetUserId();
                                pd.PaymentAmount = Convert.ToDouble(amount) * 100;
                                pd.PaymentDate = DateTime.Now;
                                pd.StripePaymentID = charge.Id;

                                fd.UserID = User.Identity.GetUserId();
                                fd.paymentdetails = pd;
                                fd.FlightType = "OneWay";
                                fd.Source = depto.Text;
                                fd.Destination = arriveo.Text;
                                fd.ArrivalDate1 = dDate.ToShortDateString();
                                fd.ArrivalTime1 = dDate.ToShortTimeString();
                                fd.DepartureDate1 = oDate.ToShortDateString();
                                fd.DepartureTime1 = oDate.ToShortTimeString();

                                using (var client = new HttpClient())
                                {
                                    string responseString="";
                                    client.BaseAddress = new Uri("http://localhost/Travelopedia-api/api/");
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                    var response = client.PostAsJsonAsync("flights/ConfirmFlight/", fd).Result;

                                    if (response.IsSuccessStatusCode)
                                    {
                                        responseString = response.Content.ReadAsStringAsync().Result;
                                    }

                                    Session["Payment"] = charges2;
                                    Session["token"] = responseString;
                                    Response.Redirect("SuccessPayment.aspx");
                                }
                            }
                            else
                            {
                                Session["Payment"] = charges2;
                                Response.Redirect("ErrorPayment.aspx");
                            }


                        }
                    }
                    else if (DataType == "hotel")
                    {
                        HotelDetails.Visible = true;
                        CarDetails.Visible = false;
                        hotelname.Text = token.SelectToken("name").ToString();
                        city.Text = token.SelectToken("city").ToString();
                        state.Text = token.SelectToken("state").ToString();
                        subtotalhotel.Text = token.SelectToken("subtotal").ToString();
                        noofguests.Text = Session["NoOfGuests"].ToString();

                        PaymentDetails pd = new PaymentDetails();
                        HotelPayment hp = new HotelPayment();

                        noofrooms.Text = token.SelectToken("rooms").ToString();
                        hoteltax.Text = token.SelectToken("taxesandfees").ToString();
                        hoteltotal.Text = token.SelectToken("totalprice").ToString();
                        subtotalhotel1.Text = token.SelectToken("subtotal").ToString();
                        hotelnamefilter.Text = hotelname.Text;
                        hotelcityfilter.Text = city.Text;
                        hotelstatefilter.Text = state.Text;
                        hotelcheckin.Text = token.SelectToken("checkindate").ToString();
                        hotelcheckout.Text = token.SelectToken("checkoutdate").ToString();

                        string latitude = token.SelectToken("centroid").ToString().Split('-')[0];
                        string longitude = token.SelectToken("centroid").ToString().Split('-')[1];

                        ScriptManager.RegisterStartupScript(Page, GetType(), "initMap", "<script>initMap(" + latitude + "," + longitude + ")</script>", false);

                        amount = (Convert.ToDouble(hoteltotal.Text) * 100).ToString();

                        if (Request.Form["stripeToken"] != null)
                        {
                            var customers = new StripeCustomerService();
                            var charges = new StripeChargeService();

                            var customer = customers.Create(new StripeCustomerCreateOptions
                            {
                                Email = Request.Form["stripeEmail"],
                                SourceToken = Request.Form["stripeToken"]
                            });

                            var charge = charges.Create(new StripeChargeCreateOptions
                            {
                                Amount = Convert.ToInt32(Convert.ToDouble(amount) * 100),
                                Description = "Sample Charge",
                                Currency = "usd",
                                CustomerId = customer.Id,
                                ReceiptEmail = customer.Email,
                                SourceTokenOrExistingSourceId = customer.DefaultSourceId
                            });

                            StripeConfiguration.SetApiKey(WebConfigurationManager.AppSettings["StripeSecretKey"]);

                            var chargeService = new StripeChargeService();
                            StripeCharge charges2 = chargeService.Get(charge.Id);

                            if (charges2.Captured == true)
                            {

                                pd.UserID = User.Identity.GetUserId();
                                pd.PaymentAmount = Convert.ToDouble(amount) * 100;
                                pd.PaymentDate = DateTime.Now;
                                pd.StripePaymentID = charge.Id;

                                hp.NumberOfRooms = Convert.ToInt32(noofrooms.Text);
                                hp.HotelName = hotelnamefilter.Text;
                                hp.NumberOfGuests = Convert.ToInt32(noofguests.Text);
                                hp.Location = hotelcityfilter.Text + ", " + hotelstatefilter.Text;
                                hp.CheckInDate = hotelcheckin.Text;
                                hp.CheckOutDate = hotelcheckout.Text;
                                hp.paymentDetails = pd;
                                hp.UserID = User.Identity.GetUserId();

                                using (var client = new HttpClient())
                                {
                                    string responseString = "";
                                    client.BaseAddress = new Uri("http://localhost/Travelopedia-api/api/");
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                    var response = client.PostAsJsonAsync("hotels/ConfirmHotelBooking/", hp).Result;

                                    if (response.IsSuccessStatusCode)
                                    {
                                        responseString = response.Content.ReadAsStringAsync().Result;
                                    }

                                    Session["Payment"] = charges2;
                                    Session["token"] = responseString;
                                    Response.Redirect("SuccessPayment.aspx");
                                }
                            }
                            else
                            {
                                Session["Payment"] = charges2;
                                Response.Redirect("ErrorPayment.aspx");
                            }


                        }
                    }
                    else
                    {
                        name.Text = token.SelectToken("PossibleModels").ToString();
                        type.Text = token.SelectToken("CarTypeName").ToString();
                        location.Text = token.SelectToken("VendorLocation").ToString();
                        location1.Text = token.SelectToken("VendorLocation").ToString();
                        price.Text = token.SelectToken("DailyRate").ToString();
                        dailyprice.Text = token.SelectToken("DailyRate").ToString();
                        Subtotal.Text = token.SelectToken("SubTotal").ToString();
                        tax.Text = token.SelectToken("TaxesAndFees").ToString();
                        total.Text = token.SelectToken("TotalPrice").ToString();
                        pickup.Text = token.SelectToken("PickupDay").ToString();
                        dropoff.Text = token.SelectToken("DropoffDay").ToString();
                        days.Text = token.SelectToken("RentalDays").ToString();
                        pickuptime.Text = token.SelectToken("PickupTime").ToString();
                        dropofftime.Text = token.SelectToken("DropoffTime").ToString();

                        PaymentDetails pd = new PaymentDetails();
                        CarBooking cb = new CarBooking();
                        amount = (Convert.ToDouble(total.Text) * 100).ToString();

                        if (Request.Form["stripeToken"] != null)
                        {
                            var customers = new StripeCustomerService();
                            var charges = new StripeChargeService();

                            var customer = customers.Create(new StripeCustomerCreateOptions
                            {
                                Email = Request.Form["stripeEmail"],
                                SourceToken = Request.Form["stripeToken"]
                            });

                            var charge = charges.Create(new StripeChargeCreateOptions
                            {
                                Amount = Convert.ToInt32(Convert.ToDouble(amount) * 100),
                                Description = "Sample Charge",
                                Currency = "usd",
                                CustomerId = customer.Id,
                                ReceiptEmail = customer.Email,
                                SourceTokenOrExistingSourceId = customer.DefaultSourceId
                            });

                            StripeConfiguration.SetApiKey(WebConfigurationManager.AppSettings["StripeSecretKey"]);

                            var chargeService = new StripeChargeService();
                            StripeCharge charges2 = chargeService.Get(charge.Id);

                            if (charges2.Captured == true)
                            {

                                pd.UserID = User.Identity.GetUserId();
                                pd.PaymentAmount = Convert.ToDouble(amount) * 100;
                                pd.PaymentDate = DateTime.Now;
                                pd.StripePaymentID = charge.Id;

                                cb.UserID = User.Identity.GetUserId();
                                cb.PossibleModels = name.Text;
                                cb.CarType = type.Text;
                                cb.paymentDetails = pd;
                                cb.PickUpDate = pickup.Text;
                                cb.PickUpLocation = location.Text;
                                cb.PickUpTime = pickuptime.Text;
                                cb.DropOffDate = dropoff.Text;
                                cb.DropOffTime = dropofftime.Text;


                                using (var client = new HttpClient())
                                {
                                    string responseString = "";
                                    client.BaseAddress = new Uri("http://localhost/Travelopedia-api/api/");
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                    var response = client.PostAsJsonAsync("car/ConfirmCarBooking/", cb).Result;

                                    if (response.IsSuccessStatusCode)
                                    {
                                        responseString = response.Content.ReadAsStringAsync().Result;
                                    }

                                    Session["Payment"] = charges2;
                                    Session["token"] = responseString;
                                    Response.Redirect("SuccessPayment.aspx");
                                }
                            }
                            else
                            {
                                Session["Payment"] = charges2;
                                Response.Redirect("ErrorPayment.aspx");
                            }

                        }
                    }
                }
                else
                {
                    //data is empty
                    Response.Redirect("Default.Aspx");
                }
            }
            else
            {
                //user is not authenticated
                Response.Redirect("Default.Aspx");
            }
        }
    }
}