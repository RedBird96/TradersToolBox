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
    /// CryptoSnapshotTickerFullBookBids
    /// </summary>
    [DataContract]
        public partial class CryptoSnapshotTickerFullBookBids :  IEquatable<CryptoSnapshotTickerFullBookBids>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoSnapshotTickerFullBookBids" /> class.
        /// </summary>
        /// <param name="p">The price of this book level..</param>
        /// <param name="x">A map of the exchange ID to number of shares at this price level. &lt;br /&gt; &lt;br /&gt; **Example:** &lt;br /&gt; &#x60;{   \&quot;p\&quot;: 16302.94,   \&quot;x\&quot;: {     \&quot;1\&quot;: 0.02859424,     \&quot;6\&quot;: 0.023455   } }&#x60; &lt;br /&gt; &lt;br /&gt; In this example, exchange ID 1 has 0.02859424 shares available at $16,302.94, and exchange ID 6 has 0.023455 shares at the same price level. .</param>
        public CryptoSnapshotTickerFullBookBids(double? p = default(double?), Object x = default(Object))
        {
            this.P = p;
            this.X = x;
        }
        
        /// <summary>
        /// The price of this book level.
        /// </summary>
        /// <value>The price of this book level.</value>
        [DataMember(Name="p", EmitDefaultValue=false)]
        public double? P { get; set; }

        /// <summary>
        /// A map of the exchange ID to number of shares at this price level. &lt;br /&gt; &lt;br /&gt; **Example:** &lt;br /&gt; &#x60;{   \&quot;p\&quot;: 16302.94,   \&quot;x\&quot;: {     \&quot;1\&quot;: 0.02859424,     \&quot;6\&quot;: 0.023455   } }&#x60; &lt;br /&gt; &lt;br /&gt; In this example, exchange ID 1 has 0.02859424 shares available at $16,302.94, and exchange ID 6 has 0.023455 shares at the same price level. 
        /// </summary>
        /// <value>A map of the exchange ID to number of shares at this price level. &lt;br /&gt; &lt;br /&gt; **Example:** &lt;br /&gt; &#x60;{   \&quot;p\&quot;: 16302.94,   \&quot;x\&quot;: {     \&quot;1\&quot;: 0.02859424,     \&quot;6\&quot;: 0.023455   } }&#x60; &lt;br /&gt; &lt;br /&gt; In this example, exchange ID 1 has 0.02859424 shares available at $16,302.94, and exchange ID 6 has 0.023455 shares at the same price level. </value>
        [DataMember(Name="x", EmitDefaultValue=false)]
        public Object X { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class CryptoSnapshotTickerFullBookBids {\n");
            sb.Append("  P: ").Append(P).Append("\n");
            sb.Append("  X: ").Append(X).Append("\n");
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
            return this.Equals(input as CryptoSnapshotTickerFullBookBids);
        }

        /// <summary>
        /// Returns true if CryptoSnapshotTickerFullBookBids instances are equal
        /// </summary>
        /// <param name="input">Instance of CryptoSnapshotTickerFullBookBids to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(CryptoSnapshotTickerFullBookBids input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.P == input.P ||
                    (this.P != null &&
                    this.P.Equals(input.P))
                ) && 
                (
                    this.X == input.X ||
                    (this.X != null &&
                    this.X.Equals(input.X))
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
                if (this.P != null)
                    hashCode = hashCode * 59 + this.P.GetHashCode();
                if (this.X != null)
                    hashCode = hashCode * 59 + this.X.GetHashCode();
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