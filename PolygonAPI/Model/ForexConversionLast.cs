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
    /// ForexConversionLast
    /// </summary>
    [DataContract]
        public partial class ForexConversionLast :  IEquatable<ForexConversionLast>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForexConversionLast" /> class.
        /// </summary>
        /// <param name="ask">The ask price..</param>
        /// <param name="bid">The bid price..</param>
        /// <param name="exchange">The exchange ID. See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_exchanges_anchor\&quot; alt&#x3D;\&quot;Exchanges\&quot;&gt;Exchanges&lt;/a&gt; for Polygon.io&#x27;s mapping of exchange IDs..</param>
        /// <param name="timestamp">The Unix Msec timestamp for the start of the aggregate window..</param>
        public ForexConversionLast(double? ask = default(double?), double? bid = default(double?), int? exchange = default(int?), int? timestamp = default(int?))
        {
            this.Ask = ask;
            this.Bid = bid;
            this.Exchange = exchange;
            this.Timestamp = timestamp;
        }
        
        /// <summary>
        /// The ask price.
        /// </summary>
        /// <value>The ask price.</value>
        [DataMember(Name="ask", EmitDefaultValue=false)]
        public double? Ask { get; set; }

        /// <summary>
        /// The bid price.
        /// </summary>
        /// <value>The bid price.</value>
        [DataMember(Name="bid", EmitDefaultValue=false)]
        public double? Bid { get; set; }

        /// <summary>
        /// The exchange ID. See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_exchanges_anchor\&quot; alt&#x3D;\&quot;Exchanges\&quot;&gt;Exchanges&lt;/a&gt; for Polygon.io&#x27;s mapping of exchange IDs.
        /// </summary>
        /// <value>The exchange ID. See &lt;a href&#x3D;\&quot;https://polygon.io/docs/get_v1_meta_exchanges_anchor\&quot; alt&#x3D;\&quot;Exchanges\&quot;&gt;Exchanges&lt;/a&gt; for Polygon.io&#x27;s mapping of exchange IDs.</value>
        [DataMember(Name="exchange", EmitDefaultValue=false)]
        public int? Exchange { get; set; }

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
            sb.Append("class ForexConversionLast {\n");
            sb.Append("  Ask: ").Append(Ask).Append("\n");
            sb.Append("  Bid: ").Append(Bid).Append("\n");
            sb.Append("  Exchange: ").Append(Exchange).Append("\n");
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
            return this.Equals(input as ForexConversionLast);
        }

        /// <summary>
        /// Returns true if ForexConversionLast instances are equal
        /// </summary>
        /// <param name="input">Instance of ForexConversionLast to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ForexConversionLast input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Ask == input.Ask ||
                    (this.Ask != null &&
                    this.Ask.Equals(input.Ask))
                ) && 
                (
                    this.Bid == input.Bid ||
                    (this.Bid != null &&
                    this.Bid.Equals(input.Bid))
                ) && 
                (
                    this.Exchange == input.Exchange ||
                    (this.Exchange != null &&
                    this.Exchange.Equals(input.Exchange))
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
                if (this.Ask != null)
                    hashCode = hashCode * 59 + this.Ask.GetHashCode();
                if (this.Bid != null)
                    hashCode = hashCode * 59 + this.Bid.GetHashCode();
                if (this.Exchange != null)
                    hashCode = hashCode * 59 + this.Exchange.GetHashCode();
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