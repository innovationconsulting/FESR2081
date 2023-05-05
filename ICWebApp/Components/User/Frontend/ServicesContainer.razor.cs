using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.Models.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ICWebApp.Components.User.Frontend
{
    public partial class ServicesContainer
    {
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Parameter] public List<ServiceDataItem> Data { get; set; }

        private Guid AccordionID = Guid.NewGuid();
        private string? _keyword;
        private string? Keyword
        {
            get
            {
                return _keyword;
            }
            set
            {
                _keyword = value;
                ShowCount = 6;
                StateHasChanged();
            }
        }
        private int ShowCount { get; set; } = 6;
        private void IncreaseShowCount()
        {
            ShowCount += 6;
            StateHasChanged();
        }
    }
}
