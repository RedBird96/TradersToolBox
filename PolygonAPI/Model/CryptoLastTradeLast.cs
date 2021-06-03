/* 
 * Polygon API
 *
 * The future of fintech.
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = PolygonIO.Client.SwaggerDateConverter;

namespace PolygonIO.Model
{
    /// <summary>
    /// CryptoLastTradeLast
    /// </summary>
    [DataContract]
        public partial class CryptoLastTradeLast :  IEquatable<CryptoLastTradeLast>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoLastTradeLast" /> class.
        /// </summary>
        /// <param name="conditions">conditions.</param>
        /// <param name="exchange">The exchange that this crypto trade happened on.   See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_crypto-exchanges_anchor\&quot;&gt;Crypto Exchanges&lt;/a&gt; for a mapping of exchanges to IDs. .</param>
        /// <param name="price">The price of the trade..</param>
        /// <param name="size">The size of the trade..</param>
        /// <param name="timestamp">The Unix Msec timestamp for the start of the aggregate window..</param>
        public CryptoLastTradeLast(List<int?> conditions = default, int? exchange = default, double? price = default, double? size = default, int? timestamp = default)
        {
            this.Conditions = conditions;
            this.Exchange = exchange;
            this.Price = price;
            this.Size = size;
            this.Timestamp = timestamp;
        }
        
        /// <summary>
        /// Gets or Sets Conditions
        /// </summary>
        [DataMember(Name="conditions", EmitDefaultValue=false)]
        public List<int?> Conditions { get; set; }

        /// <summary>
        /// The exchange that this crypto trade happened on.   See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_crypto-exchanges_anchor\&quot;&gt;Crypto Exchanges&lt;/a&gt; for a mapping of exchanges to IDs. 
        /// </summary>
        /// <value>The exchange that this crypto trade happened on.   See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_crypto-exchanges_anchor\&quot;&gt;Crypto Exchanges&lt;/a&gt; for a mapping of exchanges to IDs. </value>
        [DataMember(Name="exchange", EmitDefaultValue=false)]
        public int? Exchange { get; set; }

        /// <summary>
        /// The price of the trade.
        /// </summary>
        /// <value>The price of the trade.</value>
        [DataMember(Name="price", EmitDefaultValue=false)]
        public double? Price { get; set; }

        /// <summary>
        /// The size of the trade.
        /// </summary>
        /// <value>The size of the trade.</value>
        [DataMember(Name="size", EmitDefaultValue=false)]
        public double? Size { get; set; }

        /// <summary>
        /// The Unix Msec timestamp for the start of the aggregate window.
        /// </summary>
        /// <value>The Unix Msec timestamp for the start of the aggregate window.</value>
        [DataMember(Name="timestamp", EmitDefaultValue=false)]
        public int? Timestamp { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class CryptoLastTradeLast {\n");
            sb.Append("  Conditions: ").Append(Conditions).Append("\n");
            sb.Append("  Exchange: ").Append(Exchange).Append("\n");
            sb.Append("  Price: ").Append(Price).Append("\n");
            sb.Append("  Size: ").Append(Size).Append("\n");
            sb.Append("  Timestamp: ").Append(Timestamp).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as CryptoLastTradeLast);
        }

        /// <summary>
        /// Returns true if CryptoLastTradeLast instances are equal
        /// </summary>
        /// <param name="input">Instance of CryptoLastTradeLast to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(CryptoLastTradeLast input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Conditions == input.Conditions ||
                    this.Conditions != null &&
                    input.Conditions != null &&
                    this.Conditions.SequenceEqual(input.Conditions)
                ) && 
                (
                    this.Exchange == input.Exchange ||
                    (this.Exchange != null &&
                    this.Exchange.Equals(input.Exchange))
                ) && 
                (
                    this.Price == input.Price ||
                    (this.Price != null &&
                    this.Price.Equals(input.Price))
                ) && 
                (
                    this.Size == input.Size ||
                    (this.Size != null &&
                    this.Size.Equals(input.Size))
                ) && 
                (
                    this.Timestamp == input.Timestamp ||
                    (this.Timestamp != null &&
                    this.Timestamp.Equals(input.Timestamp))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Conditions != null)
                    hashCode = hashCode * 59 + this.Conditions.GetHashCode();
                if (this.Exchange != null)
                    hashCode = hashCode * 59 + this.Exchange.GetHashCode();
                if (this.Price != null)
                    hashCode = hashCode * 59 + this.Price.GetHashCode();
                if (this.Size != null)
                    hashCode = hashCode * 59 + this.Size.GetHashCode();
                if (this.Timestamp != null)
                    hashCode = hashCode * 59 + this.Timestamp.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}