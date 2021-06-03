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
    /// InlineResponse20029
    /// </summary>
    [DataContract]
        public partial class InlineResponse20029 :  IEquatable<InlineResponse20029>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineResponse20029" /> class.
        /// </summary>
        /// <param name="status">The status of this request&#x27;s response..</param>
        /// <param name="adjusted">Whether or not this response was adjusted for splits..</param>
        /// <param name="queryCount">The number of aggregates (minute or day) used to generate the response..</param>
        /// <param name="resultsCount">The total number of results for this request..</param>
        /// <param name="requestId">A request id assigned by the server..</param>
        /// <param name="results">results.</param>
        public InlineResponse20029(string status = default(string), bool? adjusted = default(bool?), int? queryCount = default(int?), int? resultsCount = default(int?), string requestId = default(string), List<ForexTickerResultsResults> results = default(List<ForexTickerResultsResults>))
        {
            this.Status = status;
            this.Adjusted = adjusted;
            this.QueryCount = queryCount;
            this.ResultsCount = resultsCount;
            this.RequestId = requestId;
            this.Results = results;
        }
        
        /// <summary>
        /// The status of this request&#x27;s response.
        /// </summary>
        /// <value>The status of this request&#x27;s response.</value>
        [DataMember(Name="status", EmitDefaultValue=false)]
        public string Status { get; set; }

        /// <summary>
        /// Whether or not this response was adjusted for splits.
        /// </summary>
        /// <value>Whether or not this response was adjusted for splits.</value>
        [DataMember(Name="adjusted", EmitDefaultValue=false)]
        public bool? Adjusted { get; set; }

        /// <summary>
        /// The number of aggregates (minute or day) used to generate the response.
        /// </summary>
        /// <value>The number of aggregates (minute or day) used to generate the response.</value>
        [DataMember(Name="queryCount", EmitDefaultValue=false)]
        public int? QueryCount { get; set; }

        /// <summary>
        /// The total number of results for this request.
        /// </summary>
        /// <value>The total number of results for this request.</value>
        [DataMember(Name="resultsCount", EmitDefaultValue=false)]
        public int? ResultsCount { get; set; }

        /// <summary>
        /// A request id assigned by the server.
        /// </summary>
        /// <value>A request id assigned by the server.</value>
        [DataMember(Name="request_id", EmitDefaultValue=false)]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or Sets Results
        /// </summary>
        [DataMember(Name="results", EmitDefaultValue=false)]
        public List<ForexTickerResultsResults> Results { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InlineResponse20029 {\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  Adjusted: ").Append(Adjusted).Append("\n");
            sb.Append("  QueryCount: ").Append(QueryCount).Append("\n");
            sb.Append("  ResultsCount: ").Append(ResultsCount).Append("\n");
            sb.Append("  RequestId: ").Append(RequestId).Append("\n");
            sb.Append("  Results: ").Append(Results).Append("\n");
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
            return this.Equals(input as InlineResponse20029);
        }

        /// <summary>
        /// Returns true if InlineResponse20029 instances are equal
        /// </summary>
        /// <param name="input">Instance of InlineResponse20029 to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(InlineResponse20029 input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Status == input.Status ||
                    (this.Status != null &&
                    this.Status.Equals(input.Status))
                ) && 
                (
                    this.Adjusted == input.Adjusted ||
                    (this.Adjusted != null &&
                    this.Adjusted.Equals(input.Adjusted))
                ) && 
                (
                    this.QueryCount == input.QueryCount ||
                    (this.QueryCount != null &&
                    this.QueryCount.Equals(input.QueryCount))
                ) && 
                (
                    this.ResultsCount == input.ResultsCount ||
                    (this.ResultsCount != null &&
                    this.ResultsCount.Equals(input.ResultsCount))
                ) && 
                (
                    this.RequestId == input.RequestId ||
                    (this.RequestId != null &&
                    this.RequestId.Equals(input.RequestId))
                ) && 
                (
                    this.Results == input.Results ||
                    this.Results != null &&
                    input.Results != null &&
                    this.Results.SequenceEqual(input.Results)
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
                if (this.Status != null)
                    hashCode = hashCode * 59 + this.Status.GetHashCode();
                if (this.Adjusted != null)
                    hashCode = hashCode * 59 + this.Adjusted.GetHashCode();
                if (this.QueryCount != null)
                    hashCode = hashCode * 59 + this.QueryCount.GetHashCode();
                if (this.ResultsCount != null)
                    hashCode = hashCode * 59 + this.ResultsCount.GetHashCode();
                if (this.RequestId != null)
                    hashCode = hashCode * 59 + this.RequestId.GetHashCode();
                if (this.Results != null)
                    hashCode = hashCode * 59 + this.Results.GetHashCode();
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
