/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrimpNet.Net
{
    /// <summary>
    /// List of IANA Registered Media Types as string. (e.g. application/msword, image/jpg)
    /// </summary>
    /// <remarks>
    /// http://www.iana.org/assignments/media-types/
    /// <para>
    /// http://www.dailycoding.com/Posts/mime_contenttypes_with_file_extension.aspx
    /// </para>
    /// </remarks>
    public class MimeMediaType
    {
        /// <summary>
        /// Constantstrings for MIME application/...
        /// </summary>
        public class application
        {
            ///<summary>
            /// Place holder for 'Image Not Found'(application/octet-stream)
            ///</summary>
            public const string inf = "application/octet-stream";
            ///<summary>
            /// AutoCAD(application/acad)
            ///</summary>
            public const string dwg = "application/acad";
            ///<summary>
            /// compressed archive(application/arj)
            ///</summary>
            public const string arj = "application/arj";
            ///<summary>
            /// Astound(application/astound)
            ///</summary>
            public const string asd = "application/astound";
            ///<summary>
            /// Astound(application/astound)
            ///</summary>
            public const string asn = "application/astound";
            ///<summary>
            /// ClarisCAD(application/clariscad)
            ///</summary>
            public const string ccad = "application/clariscad";
            ///<summary>
            /// Microsoft Excel(application/msexcel)
            ///</summary>
            public const string csv = "application/msexcel";
            ///<summary>
            /// MATRA Prelude drafting(application/drafting)
            ///</summary>
            public const string drw = "application/drafting";
            ///<summary>
            /// DXF (AutoCAD)(application/dxf)
            ///</summary>
            public const string dxf = "application/dxf";
            ///<summary>
            /// SDRC I-DEAS (application/i-deas)
            ///</summary>
            public const string unv = "application/i-deas";
            ///<summary>
            /// IGES graphics format(application/iges)
            ///</summary>
            public const string iges = "application/iges";
            ///<summary>
            /// IGES graphics format(application/iges)
            ///</summary>
            public const string igs = "application/iges";
            ///<summary>
            /// Java archive(application/java-archive)
            ///</summary>
            public const string jar = "application/java-archive";
            ///<summary>
            /// Macintosh binary BinHex 4.0(application/mac-binhex40)
            ///</summary>
            public const string hqx = "application/mac-binhex40";
            ///<summary>
            /// Microsoft Access(application/msaccess)
            ///</summary>
            public const string mdb = "application/msaccess";
            ///<summary>
            /// Microsoft Excel(application/msexcel)
            ///</summary>
            public const string xla = "application/msexcel";
            ///<summary>
            /// Microsoft Excel(application/msexcel)
            ///</summary>
            public const string xls = "application/msexcel";
            ///<summary>
            /// Microsoft Excel(application/msexcel)
            ///</summary>
            public const string xlt = "application/msexcel";
            ///<summary>
            /// Microsoft Excel(application/msexcel)
            ///</summary>
            public const string xlw = "application/msexcel";
            ///<summary>
            /// Microsoft PowerPoint(application/mspowerpoint)
            ///</summary>
            public const string pot = "application/mspowerpoint";
            ///<summary>
            /// Microsoft PowerPoint(application/mspowerpoint)
            ///</summary>
            public const string pps = "application/mspowerpoint";
            ///<summary>
            /// Microsoft PowerPoint(application/mspowerpoint)
            ///</summary>
            public const string ppt = "application/mspowerpoint";
            ///<summary>
            /// Microsoft Project(application/msproject)
            ///</summary>
            public const string mpp = "application/msproject";
            ///<summary>
            /// Microsoft Word(application/msword)
            ///</summary>
            public const string doc = "application/msword";
            ///<summary>
            /// Microsoft Word(application/msword)
            ///</summary>
            public const string word = "application/msword";
            ///<summary>
            /// Microsoft Word(application/msword)
            ///</summary>
            public const string w6w = "application/msword";
            ///<summary>
            /// Microsoft Write(application/mswrite)
            ///</summary>
            public const string wri = "application/mswrite";
            ///<summary>
            /// ODA(application/oda)
            ///</summary>
            public const string oda = "application/oda";
            ///<summary>
            /// Adobe Acrobat(application/pdf)
            ///</summary>
            public const string pdf = "application/pdf";
            ///<summary>
            /// PostScript(application/postscript)
            ///</summary>
            public const string ai = "application/postscript";
            ///<summary>
            /// PostScript(application/postscript)
            ///</summary>
            public const string eps = "application/postscript";
            ///<summary>
            /// PostScript(application/postscript)
            ///</summary>
            public const string ps = "application/postscript";
            ///<summary>
            /// PTC Pro/ENGINEER(application/pro_eng)
            ///</summary>
            public const string part = "application/pro_eng";
            ///<summary>
            /// PTC Pro/ENGINEER(application/pro_eng)
            ///</summary>
            public const string prt = "application/pro_eng";
            ///<summary>
            /// Rich Text Format(application/rtf)
            ///</summary>
            public const string rtf = "application/rtf";
            ///<summary>
            /// SET (French CAD)(application/set)
            ///</summary>
            public const string set = "application/set";
            ///<summary>
            /// stereolithography(application/sla)
            ///</summary>
            public const string stl = "application/sla";
            ///<summary>
            /// MATRA Prelude Solids(application/solids)
            ///</summary>
            public const string sol = "application/solids";
            ///<summary>
            /// ISO-10303 STEP data(application/STEP)
            ///</summary>
            public const string st = "application/STEP";
            ///<summary>
            /// ISO-10303 STEP data(application/STEP)
            ///</summary>
            public const string step = "application/STEP";
            ///<summary>
            /// ISO-10303 STEP data(application/STEP)
            ///</summary>
            public const string stp = "application/STEP";
            ///<summary>
            /// VDA-FS Surface data(application/vda)
            ///</summary>
            public const string vda = "application/vda";
            ///<summary>
            /// binary CPIO(application/x-bcpio)
            ///</summary>
            public const string bcpio = "application/x-bcpio";
            ///<summary>
            /// POSIX CPIO(application/x-cpio)
            ///</summary>
            public const string cpio = "application/x-cpio";
            ///<summary>
            /// C-shell script(application/x-csh)
            ///</summary>
            public const string csh = "application/x-csh";
            ///<summary>
            /// Macromedia Director(application/x-director)
            ///</summary>
            public const string dcr = "application/x-director";
            ///<summary>
            /// Macromedia Director(application/x-director)
            ///</summary>
            public const string dir = "application/x-director";
            ///<summary>
            /// Macromedia Director(application/x-director)
            ///</summary>
            public const string dxr = "application/x-director";
            ///<summary>
            /// TeX DVI(application/x-dvi)
            ///</summary>
            public const string dvi = "application/x-dvi";
            ///<summary>
            /// AutoCAD(application/x-dwf)
            ///</summary>
            public const string dwf = "application/x-dwf";
            ///<summary>
            /// GNU tar(application/x-gtar)
            ///</summary>
            public const string gtar = "application/x-gtar";
            ///<summary>
            /// GNU ZIP(application/x-gzip)
            ///</summary>
            public const string gz = "application/x-gzip";
            ///<summary>
            /// NCSA HDF Data File(application/x-hdf)
            ///</summary>
            public const string hdf = "application/x-hdf";
            ///<summary>
            /// JavaScript(application/x-javascript)
            ///</summary>
            public const string js = "application/x-javascript";
            ///<summary>
            /// LaTeX source(application/x-latex)
            ///</summary>
            public const string latex = "application/x-latex";
            ///<summary>
            /// Macintosh compressed(application/x-macbinary)
            ///</summary>
            public const string bin = "application/x-macbinary";
            ///<summary>
            /// FrameMaker MIF (application/x-mif)
            ///</summary>
            public const string mif = "application/x-mif";
            ///<summary>
            /// Unidata netCDF(application/x-netcdf)
            ///</summary>
            public const string cdf = "application/x-netcdf";
            ///<summary>
            /// Unidata netCDF(application/x-netcdf)
            ///</summary>
            public const string nc = "application/x-netcdf";
            ///<summary>
            /// Bourne shell script(application/x-sh)
            ///</summary>
            public const string sh = "application/x-sh";
            ///<summary>
            /// shell archive(application/x-shar)
            ///</summary>
            public const string shar = "application/x-shar";
            ///<summary>
            /// Macromedia Shockwave(application/x-shockwave-flash)
            ///</summary>
            public const string swf = "application/x-shockwave-flash";
            ///<summary>
            /// StuffIt archive(application/x-stuffit)
            ///</summary>
            public const string sit = "application/x-stuffit";
            ///<summary>
            /// SVR4 CPIO(application/x-sv4cpio)
            ///</summary>
            public const string sv4cpio = "application/x-sv4cpio";
            ///<summary>
            /// SVR4 CPIO with CRC(application/x-sv4crc)
            ///</summary>
            public const string sv4crc = "application/x-sv4crc";
            ///<summary>
            /// 4.3BSD tar format(application/x-tar)
            ///</summary>
            public const string tar = "application/x-tar";
            ///<summary>
            /// TCL script(application/x-tcl)
            ///</summary>
            public const string tcl = "application/x-tcl";
            ///<summary>
            /// TeX source(application/x-tex)
            ///</summary>
            public const string tex = "application/x-tex";
            ///<summary>
            /// Texinfo (Emacs)(application/x-texinfo)
            ///</summary>
            public const string texi = "application/x-texinfo";
            ///<summary>
            /// Texinfo (Emacs)(application/x-texinfo)
            ///</summary>
            public const string texinfo = "application/x-texinfo";
            ///<summary>
            /// Troff(application/x-troff)
            ///</summary>
            public const string roff = "application/x-troff";
            ///<summary>
            /// Troff(application/x-troff)
            ///</summary>
            public const string t = "application/x-troff";
            ///<summary>
            /// Troff(application/x-troff)
            ///</summary>
            public const string tr = "application/x-troff";
            ///<summary>
            /// Troff with MAN macros(application/x-troff-man)
            ///</summary>
            public const string man = "application/x-troff-man";
            ///<summary>
            /// Troff with ME macros(application/x-troff-me)
            ///</summary>
            public const string me = "application/x-troff-me";
            ///<summary>
            /// Troff with MS macros(application/x-troff-ms)
            ///</summary>
            public const string ms = "application/x-troff-ms";
            ///<summary>
            /// POSIX tar format(application/x-ustar)
            ///</summary>
            public const string ustar = "application/x-ustar";
            ///<summary>
            /// WAIS source(application/x-wais-source)
            ///</summary>
            public const string src = "application/x-wais-source";
            ///<summary>
            /// Microsoft Windows help(application/x-winhelp)
            ///</summary>
            public const string hlp = "application/x-winhelp";
            ///<summary>
            /// ZIP archive(application/zip)
            ///</summary>
            public const string zip = "application/zip";
            ///<summary>
            /// Microsoft Office Word 2007 document(application/vnd.openxmlformats-officedocument.wordprocessingml.document)
            ///</summary>
            public const string docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            ///<summary>
            /// Office Word 2007 macro-enabled document(application/vnd.ms-word.document.macroEnabled.12)
            ///</summary>
            public const string docm = "application/vnd.ms-word.document.macroEnabled.12";
            ///<summary>
            /// Office Word 2007 template(application/vnd.openxmlformats-officedocument.wordprocessingml.template)
            ///</summary>
            public const string dotx = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
            ///<summary>
            /// Office Word 2007 macro-enabled document template(application/vnd.ms-word.template.macroEnabled.12)
            ///</summary>
            public const string dotm = "application/vnd.ms-word.template.macroEnabled.12";
            ///<summary>
            /// Microsoft Office Excel 2007 workbook(application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)
            ///</summary>
            public const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            ///<summary>
            /// Office Excel 2007 macro-enabled workbook(application/vnd.ms-excel.sheet.macroEnabled.12)
            ///</summary>
            public const string xlsm = "application/vnd.ms-excel.sheet.macroEnabled.12";
            ///<summary>
            /// Office Excel 2007 template(application/vnd.openxmlformats-officedocument.spreadsheetml.template)
            ///</summary>
            public const string xltx = "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
            ///<summary>
            /// Office Excel 2007 macro-enabled workbook template(application/vnd.ms-excel.template.macroEnabled.12)
            ///</summary>
            public const string xltm = "application/vnd.ms-excel.template.macroEnabled.12";
            ///<summary>
            /// Office Excel 2007 binary workbook(application/vnd.ms-excel.sheet.binary.macroEnabled.12)
            ///</summary>
            public const string xlsb = "application/vnd.ms-excel.sheet.binary.macroEnabled.12";
            ///<summary>
            /// Office Excel 2007 add-in(application/vnd.ms-excel.addin.macroEnabled.12)
            ///</summary>
            public const string xlam = "application/vnd.ms-excel.addin.macroEnabled.12";
            ///<summary>
            /// Microsoft Office PowerPoint 2007 presentation(application/vnd.openxmlformats-officedocument.presentationml.presentation)
            ///</summary>
            public const string pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            ///<summary>
            /// Office PowerPoint 2007 macro-enabled presentation(application/vnd.ms-powerpoint.presentation.macroEnabled.12)
            ///</summary>
            public const string pptm = "application/vnd.ms-powerpoint.presentation.macroEnabled.12";
            ///<summary>
            /// Office PowerPoint 2007 slide show(application/vnd.openxmlformats-officedocument.presentationml.slideshow)
            ///</summary>
            public const string ppsx = "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
            ///<summary>
            /// Office PowerPoint 2007 macro-enabled slide show(application/vnd.ms-powerpoint.slideshow.macroEnabled.12)
            ///</summary>
            public const string ppsm = "application/vnd.ms-powerpoint.slideshow.macroEnabled.12";
            ///<summary>
            /// Office PowerPoint 2007 template(application/vnd.openxmlformats-officedocument.presentationml.template)
            ///</summary>
            public const string potx = "application/vnd.openxmlformats-officedocument.presentationml.template";
            ///<summary>
            /// Office PowerPoint 2007 macro-enabled presentation template(application/vnd.ms-powerpoint.template.macroEnabled.12)
            ///</summary>
            public const string potm = "application/vnd.ms-powerpoint.template.macroEnabled.12";
            ///<summary>
            /// Office PowerPoint 2007 add-in(application/vnd.ms-powerpoint.addin.macroEnabled.12)
            ///</summary>
            public const string ppam = "application/vnd.ms-powerpoint.addin.macroEnabled.12";
            ///<summary>
            /// Office PowerPoint 2007 slide(application/vnd.openxmlformats-officedocument.presentationml.slide)
            ///</summary>
            public const string sldx = "application/vnd.openxmlformats-officedocument.presentationml.slide";
            ///<summary>
            /// Office PowerPoint 2007 macro-enabled slide(application/vnd.ms-powerpoint.slide.macroEnabled.12)
            ///</summary>
            public const string sldm = "application/vnd.ms-powerpoint.slide.macroEnabled.12";
            ///<summary>
            /// Microsoft Office OneNote 2007 section(application/onenote)
            ///</summary>
            public const string one = "application/onenote";
            ///<summary>
            /// Office OneNote 2007 TOC(application/onenote)
            ///</summary>
            public const string onetoc2 = "application/onenote";
            ///<summary>
            /// Office OneNote 2007 temporary file(application/onenote)
            ///</summary>
            public const string onetmp = "application/onenote";
            ///<summary>
            /// Office OneNote 2007 package(application/onenote)
            ///</summary>
            public const string onepkg = "application/onenote";
            ///<summary>
            /// 2007 Office system release theme(application/vnd.ms-officetheme)
            ///</summary>
            public const string thmx = "application/vnd.ms-officetheme";
        }// class application

        /// <summary>
        /// Constant strings for MIME audio/...
        /// </summary>
        public class audio
        {
            ///<summary>
            /// BASIC audio (u-law)(audio/basic)
            ///</summary>
            public const string au = "audio/basic";
            ///<summary>
            /// BASIC audio (u-law)(audio/basic)
            ///</summary>
            public const string snd = "audio/basic";
            ///<summary>
            /// MIDI(audio/midi)
            ///</summary>
            public const string mid = "audio/midi";
            ///<summary>
            /// MIDI(audio/midi)
            ///</summary>
            public const string midi = "audio/midi";
            ///<summary>
            /// AIFF audio(audio/x-aiff)
            ///</summary>
            public const string aif = "audio/x-aiff";
            ///<summary>
            /// AIFF audio(audio/x-aiff)
            ///</summary>
            public const string aifc = "audio/x-aiff";
            ///<summary>
            /// AIFF audio(audio/x-aiff)
            ///</summary>
            public const string aiff = "audio/x-aiff";
            ///<summary>
            /// MPEG audio(audio/x-mpeg)
            ///</summary>
            public const string mp3 = "audio/x-mpeg";
            ///<summary>
            /// RealAudio(audio/x-pn-realaudio)
            ///</summary>
            public const string ra = "audio/x-pn-realaudio";
            ///<summary>
            /// RealAudio(audio/x-pn-realaudio)
            ///</summary>
            public const string ram = "audio/x-pn-realaudio";
            ///<summary>
            /// RealAudio plug-in(audio/x-pn-realaudio-plugin)
            ///</summary>
            public const string rpm = "audio/x-pn-realaudio-plugin";
            ///<summary>
            /// Voice(audio/x-voice)
            ///</summary>
            public const string voc = "audio/x-voice";
            ///<summary>
            /// Microsoft Windows WAVE audio(audio/x-wav)
            ///</summary>
            public const string wav = "audio/x-wav";
        }// class audio

        /// <summary>
        /// Constant strings for image/...
        /// </summary>
        public class image
        {
            ///<summary>
            /// Bitmap(image/bmp)
            ///</summary>
            public const string bmp = "image/bmp";
            ///<summary>
            /// GIF image(image/gif)
            ///</summary>
            public const string gif = "image/gif";
            ///<summary>
            /// Image Exchange Format(image/ief)
            ///</summary>
            public const string ief = "image/ief";
            ///<summary>
            /// JPEG image(image/jpeg)
            ///</summary>
            public const string jpe = "image/jpeg";
            ///<summary>
            /// JPEG image(image/jpeg)
            ///</summary>
            public const string jpeg = "image/jpeg";
            ///<summary>
            /// JPEG image(image/jpeg)
            ///</summary>
            public const string jpg = "image/jpeg";
            ///<summary>
            /// Macintosh PICT(image/pict)
            ///</summary>
            public const string pict = "image/pict";
            ///<summary>
            /// Portable Network Graphic(image/png)
            ///</summary>
            public const string png = "image/png";
            ///<summary>
            /// TIFF image(image/tiff)
            ///</summary>
            public const string tif = "image/tiff";
            ///<summary>
            /// TIFF image(image/tiff)
            ///</summary>
            public const string tiff = "image/tiff";
            ///<summary>
            /// CMU raster(image/x-cmu-raster)
            ///</summary>
            public const string ras = "image/x-cmu-raster";
            ///<summary>
            /// PBM Anymap format(image/x-portable-anymap)
            ///</summary>
            public const string pnm = "image/x-portable-anymap";
            ///<summary>
            /// PBM Bitmap format(image/x-portable-bitmap)
            ///</summary>
            public const string pbm = "image/x-portable-bitmap";
            ///<summary>
            /// PBM Graymap format(image/x-portable-graymap)
            ///</summary>
            public const string pgm = "image/x-portable-graymap";
            ///<summary>
            /// PBM Pixmap format(image/x-portable-pixmap)
            ///</summary>
            public const string ppm = "image/x-portable-pixmap";
            ///<summary>
            /// RGB image(image/x-rgb)
            ///</summary>
            public const string rgb = "image/x-rgb";
            ///<summary>
            /// X Bitmap(image/x-xbitmap)
            ///</summary>
            public const string xbm = "image/x-xbitmap";
            ///<summary>
            /// X Pixmap(image/x-xpixmap)
            ///</summary>
            public const string xpm = "image/x-xpixmap";
            ///<summary>
            /// X Window System dump(image/x-xwindowdump)
            ///</summary>
            public const string xwd = "image/x-xwindowdump";
        }// class image

        /// <summary>
        /// constant strings for MIME mulipart/...
        /// </summary>
        public class multipart
        {
            ///<summary>
            /// GNU ZIP archive(multipart/x-gzip)
            ///</summary>
            public const string gzip = "multipart/x-gzip";
        }// class multipart

        /// <summary>
        /// constant strings for MIME text/...
        /// </summary>
        public class text
        {
            ///<summary>
            /// HTML(text/html)
            ///</summary>
            public const string htm = "text/html";
            ///<summary>
            /// HTML(text/html)
            ///</summary>
            public const string html = "text/html";
            ///<summary>
            /// plain text(text/plain)
            ///</summary>
            public const string C = "text/plain";
            ///<summary>
            /// plain text(text/plain)
            ///</summary>
            public const string cc = "text/plain";
            ///<summary>
            /// plain text(text/plain)
            ///</summary>
            public const string h = "text/plain";
            ///<summary>
            /// plain text(text/plain)
            ///</summary>
            public const string txt = "text/plain";
            ///<summary>
            /// MIME Richtext(text/richtext)
            ///</summary>
            public const string rtx = "text/richtext";
            ///<summary>
            /// text with tabs(text/tab-separated-values)
            ///</summary>
            public const string tsv = "text/tab-separated-values";
            ///<summary>
            /// Structurally Enhanced Text(text/x-setext)
            ///</summary>
            public const string etx = "text/x-setext";
            ///<summary>
            /// SGML(text/x-sgml)
            ///</summary>
            public const string sgm = "text/x-sgml";
            ///<summary>
            /// SGML(text/x-sgml)
            ///</summary>
            public const string sgml = "text/x-sgml";
        }// class text

        /// <summary>
        /// constant strings for MIME video/...
        /// </summary>
        public class video
        {
            ///<summary>
            /// MPEG video(video/mpeg)
            ///</summary>
            public const string mpe = "video/mpeg";
            ///<summary>
            /// MPEG video(video/mpeg)
            ///</summary>
            public const string mpeg = "video/mpeg";
            ///<summary>
            /// MPEG video(video/mpeg)
            ///</summary>
            public const string mpg = "video/mpeg";
            ///<summary>
            /// Microsoft Windows video(video/msvideo)
            ///</summary>
            public const string avi = "video/msvideo";
            ///<summary>
            /// QuickTime video(video/quicktime)
            ///</summary>
            public const string mov = "video/quicktime";
            ///<summary>
            /// QuickTime video(video/quicktime)
            ///</summary>
            public const string qt = "video/quicktime";
            ///<summary>
            /// VDO streaming video(video/vdo)
            ///</summary>
            public const string vdo = "video/vdo";
            ///<summary>
            /// VIVO streaming video(video/vivo)
            ///</summary>
            public const string viv = "video/vivo";
            ///<summary>
            /// VIVO streaming video(video/vivo)
            ///</summary>
            public const string vivo = "video/vivo";
            ///<summary>
            /// SGI Movieplayer format(video/x-sgi-movie)
            ///</summary>
            public const string movie = "video/x-sgi-movie";
        }// class video

        /// <summary>
        /// Constant strings for MIME xconference/...
        /// </summary>
        public class xconference
        {
            ///<summary>
            /// CoolTalk(x-conference/x-cooltalk)
            ///</summary>
            public const string ice = "x-conference/x-cooltalk";
        }// class x-conference

        /// <summary>
        /// Constant strings for MIME xworld/...
        /// </summary>
        public class xworld
        {
            ///<summary>
            /// Virtual reality(x-world/x-svr)
            ///</summary>
            public const string svr = "x-world/x-svr";
            ///<summary>
            /// VRML Worlds(x-world/x-vrml)
            ///</summary>
            public const string wrl = "x-world/x-vrml";
            ///<summary>
            /// Virtual reality(x-world/x-vrt)
            ///</summary>
            public const string vrt = "x-world/x-vrt";
        }// class x-world
    } //class MediaTypes


}
