using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SchoolManagementSystem.Services
{
    public class CashfreeService
    {
        private readonly string _appId = "YOUR_API_KEY_HERE";
        private readonly string _secretKey = "YOUR_SECRET_KEY_HERE";
        private readonly string _baseUrl = "https://sandbox.cashfree.com/pg";
        private readonly string _returnDomain;

        // Constructor with domain parameter
        public CashfreeService(string returnDomain = null)
        {
            _returnDomain = returnDomain ?? GetDefaultDomain();
        }

        // Fallback if domain not provided
        private string GetDefaultDomain()
        {
            try
            {
                var configDomain = System.Configuration.ConfigurationManager.AppSettings["AppDomain"];
                if (!string.IsNullOrEmpty(configDomain))
                    return configDomain;
            }
            catch { }

            return "https://yourdomain.com";
        }

        public async Task<CashfreeOrderResponse> CreateOrder(
            decimal amount,
            int paymentId,
            string customerName,
            string customerEmail,
            string customerPhone)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.Add("x-client-id", _appId);
                    client.DefaultRequestHeaders.Add("x-client-secret", _secretKey);
                    client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");

                    customerEmail = !string.IsNullOrWhiteSpace(customerEmail) ? customerEmail : "noemail@example.com";
                    customerPhone = !string.IsNullOrWhiteSpace(customerPhone) ? customerPhone : "9999999999";

                    var orderRequest = new
                    {
                        order_amount = amount,
                        order_currency = "INR",
                        order_id = $"ORDER_{paymentId}_{DateTime.Now.Ticks}",
                        customer_details = new
                        {
                            customer_id = $"CUST_{paymentId}",
                            customer_name = customerName,
                            customer_email = customerEmail,
                            customer_phone = customerPhone
                        },
                        order_meta = new
                        {
                            return_url = $"{_returnDomain}/Parent/PaymentCallback?payment_id={paymentId}",
                            notify_url = $"{_returnDomain}/Parent/PaymentWebhook"
                        }
                    };

                    var json = JsonConvert.SerializeObject(orderRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] Domain: {_returnDomain}");
                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] Order Request: {json}");

                    var response = await client.PostAsync($"{_baseUrl}/orders", content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] Response: {responseString}");

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<CashfreeOrderResponse>(responseString);
                    }
                    else
                    {
                        throw new Exception($"Cashfree API Error ({response.StatusCode}): {responseString}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CASHFREE] Network Error: {ex.Message}");
                throw new Exception($"Network error connecting to Cashfree: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CASHFREE] Timeout Error: {ex.Message}");
                throw new Exception($"Request timeout connecting to Cashfree: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CASHFREE] CreateOrder Exception: {ex}");
                throw;
            }
        }

        public async Task<CashfreePaymentStatus> GetPaymentStatus(string orderId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("x-client-id", _appId);
                    client.DefaultRequestHeaders.Add("x-client-secret", _secretKey);
                    client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");

                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] Getting payment status for order: {orderId}");

                    var response = await client.GetAsync($"{_baseUrl}/orders/{orderId}");
                    var responseString = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] GetPaymentStatus Response: {responseString}");

                    if (response.IsSuccessStatusCode)
                    {
                        var orderStatus = JsonConvert.DeserializeObject<CashfreePaymentStatus>(responseString);

                        if (orderStatus.OrderStatus == "PAID")
                        {
                            var paymentDetails = await GetPaymentDetails(orderId);
                            if (paymentDetails != null && paymentDetails.Count > 0)
                            {
                                orderStatus.PaymentMethod = paymentDetails[0].PaymentGroup;
                                orderStatus.PaymentMethodDetails = paymentDetails[0].PaymentMethod;
                            }
                        }

                        return orderStatus;
                    }
                    else
                    {
                        throw new Exception($"Cashfree API Error ({response.StatusCode}): {responseString}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CASHFREE] GetPaymentStatus Exception: {ex}");
                throw;
            }
        }

        private async Task<List<PaymentDetail>> GetPaymentDetails(string orderId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("x-client-id", _appId);
                    client.DefaultRequestHeaders.Add("x-client-secret", _secretKey);
                    client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");

                    var response = await client.GetAsync($"{_baseUrl}/orders/{orderId}/payments");
                    var responseString = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[CASHFREE] GetPaymentDetails Response: {responseString}");

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<PaymentDetail>>(responseString);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CASHFREE] Error fetching payment details: {ex.Message}");
            }

            return new List<PaymentDetail>();
        }
    }

    public class CashfreeOrderResponse
    {
        [JsonProperty("cf_order_id")]
        public string CfOrderId { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("payment_session_id")]
        public string PaymentSessionId { get; set; }

        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }

        [JsonProperty("order_token")]
        public string OrderToken { get; set; }
    }

    public class CashfreePaymentStatus
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }

        [JsonProperty("order_amount")]
        public decimal OrderAmount { get; set; }

        [JsonProperty("cf_order_id")]
        public string CfOrderId { get; set; }

        public string PaymentMethod { get; set; }
        public string PaymentMethodDetails { get; set; }
    }

    public class PaymentDetail
    {
        [JsonProperty("cf_payment_id")]
        public string CfPaymentId { get; set; }

        [JsonProperty("payment_status")]
        public string PaymentStatus { get; set; }

        [JsonProperty("payment_amount")]
        public decimal PaymentAmount { get; set; }

        [JsonProperty("payment_group")]
        public string PaymentGroup { get; set; }

        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonProperty("payment_time")]
        public string PaymentTime { get; set; }
    }
}