using System;
using System.Collections.Generic;
using System.Text;

namespace DMS.BLL.Dtos
{
    public class DocumentSummaryResultDto
    {
        public record DocumentSummaryResult(Guid DocumentId, string Summary);
    }
}
