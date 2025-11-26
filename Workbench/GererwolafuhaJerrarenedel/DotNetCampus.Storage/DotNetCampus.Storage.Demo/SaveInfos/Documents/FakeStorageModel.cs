using DotNetCampus.Storage.Documents.StorageModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Demo.SaveInfos;

class FakeStorageModel : StorageModel
{
    public TestDocumentSaveInfo? Document { get; set; }

    public PresentationSaveInfo? Presentation { get; set; }

    public List<SlideSaveInfo>? SlideList { get; set; }
}