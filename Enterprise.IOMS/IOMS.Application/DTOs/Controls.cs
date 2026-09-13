using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Application.DTOs
{
    //public class ControlDto<T> where T : class
    //{
    //    public MudAutocomplete<T> ControlRef { get; set; } = default!;
    //    public T? SelectedItem { get; set; }
    //    public T? LastSelectedItem { get; set; }
    //    public string? SelectedText { get; set; }
    //    public int SelectedIndex { get; set; } = 0;

    //}

    public class ControlDto
    {
        public MudAutocomplete<ControlItemsDto> ControlRef { get; set; } = default!;
        public ControlItemsDto? SelectedItem { get; set; }
        public ControlItemsDto? LastSelectedItem { get; set; }
        public string? SelectedText { get; set; }
        public int SelectedIndex { get; set; } = 0;

    }

    public class ControlItemsDto
    { 
        public dynamic Id { get;set;  }
        public string Name { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public bool IsSelected { get; set; }
    }
}
