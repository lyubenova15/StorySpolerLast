using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace StorySpoiler.Models
{
    class ApiResponseDTO
    {
        [JsonPropertyName("msg")]

        public string? Msg { get; set; }

        [JsonPropertyName("id")]

        public string? Id { get; set; }
    }
}
