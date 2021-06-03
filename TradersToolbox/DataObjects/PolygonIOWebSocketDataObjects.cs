using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.DataObjects
{
    class PolygonIOWebSocketQuote
    {
        [JsonPropertyName("ev")]
        public string Event { get; set; }

        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("bx")]
        public int BidExchange { get; set; }

        [JsonPropertyName("bp")]
        public double BidPrice { get; set; }

        [JsonPropertyName("bs")]
        public int BidSize { get; set; }

        [JsonPropertyName("ax")]
        public int AskExchange { get; set; }

        [JsonPropertyName("ap")]
        public double AskPrice { get; set; }

        [JsonPropertyName("as")]
        public int AskSize { get; set; }

        //[JsonPropertyName("c")]
        //public int Condition { get; set; }

        [JsonPropertyName("t")]
        public long Timestamp { get; set; }


        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    class PolygonIOWebSocketTrade
    {
        [JsonPropertyName("ev")]
        public string Event { get; set; }

        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("x")]
        public int Exchange { get; set; }

        [JsonPropertyName("i")]
        public string Id { get; set; }

        [JsonPropertyName("z")]
        public int Tape { get; set; }

        [JsonPropertyName("p")]
        public decimal Price { get; set; }

        [JsonPropertyName("s")]
        public int Size { get; set; }

        [JsonPropertyName("c")]
        public int[] Conditions { get; set; }

        [JsonPropertyName("t")]
        public long Timestamp { get; set; }


        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class PolygonIOWebSocketAggregate
    {
        /// <summary>
        /// The event type
        /// </summary>
        [JsonPropertyName("ev")]
        public string Event { get; set; }

        /// <summary>
        /// The ticker symbol for the given stock
        /// </summary>
        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        /// <summary>
        /// The tick volume
        /// </summary>
        [JsonPropertyName("v")]
        public int Volume { get; set; }

        /// <summary>
        /// Today's accumulated volume
        /// </summary>
        [JsonPropertyName("av")]
        public int TodayAccVolume { get; set; }

        /// <summary>
        /// Today's official opening price
        /// </summary>
        [JsonPropertyName("op")]
        public decimal TodayOpenPrice { get; set; }

        /// <summary>
        /// Today's volume weighted average price
        /// </summary>
        [JsonPropertyName("vw")]
        public decimal TodayAveragePrice { get; set; }

        /// <summary>
        /// The opening tick price for this aggregate window
        /// </summary>
        [JsonPropertyName("o")]
        public decimal Open { get; set; }

        /// <summary>
        /// The closing tick price for this aggregate window
        /// </summary>
        [JsonPropertyName("c")]
        public decimal Close { get; set; }

        /// <summary>
        /// The highest tick price for this aggregate window
        /// </summary>
        [JsonPropertyName("h")]
        public decimal High { get; set; }

        /// <summary>
        /// The lowest tick price for this aggregate window
        /// </summary>
        [JsonPropertyName("l")]
        public decimal Low { get; set; }

        /// <summary>
        /// The tick's volume weighted average price
        /// </summary>
        [JsonPropertyName("a")]
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// The average trade size for this aggregate window
        /// </summary>
        [JsonPropertyName("z")]
        public int AverageTradeSize { get; set; }

        /// <summary>
        /// The timestamp of the starting tick for this aggregate window in Unix Milliseconds
        /// </summary>
        [JsonPropertyName("s")]
        public long AggregateStartTime { get; set; }

        /// <summary>
        /// The timestamp of the ending tick for this aggregate window in Unix Milliseconds
        /// </summary>
        [JsonPropertyName("e")]
        public long AggregateEndTime { get; set; }


        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
