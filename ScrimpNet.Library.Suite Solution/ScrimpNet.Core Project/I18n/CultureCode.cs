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

namespace ScrimpNet.i18N
{
    /// <summary>
    /// List of RFC 1766 culture codes
    /// </summary>
    public enum CultureCode
    {

        /// <summary>
        /// CultureCode is not defined
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Invariant
        /// </summary>
        Invariant = 0x007F,

            ///<summary> 
            /// Afrikaans
            ///</summary>
            af = 0x0036,

            ///<summary>
            /// Afrikaans - South Africa
            ///</summary>
            af_ZA = 0x0436,

            ///<summary>
            ///Albanian
            ///</summary>
            sq = 0x001C,

            ///<summary>
            ///Albanian - Albania
            ///</summary>
            sq_AL = 0x041C,

            ///<summary>
            ///Arabic
            ///</summary>
            ar = 0x0001,

            ///<summary>
            ///Arabic - Algeria
            ///</summary>
            ar_DZ = 0x1401,

            ///<summary>
            ///Arabic - Bahrain
            ///</summary>
            ar_BH = 0x3C01,

            ///<summary>
            ///Arabic - Egypt
            ///</summary>
            ar_EG = 0x0C01,

            ///<summary>
            ///Arabic - Iraq
            ///</summary>
            ar_IQ = 0x0801,

            ///<summary>
            ///Arabic - Jordan
            ///</summary>
            ar_JO = 0x2C01,

            ///<summary>
            ///Arabic - Kuwait
            ///</summary>
            ar_KW = 0x3401,

            ///<summary>
            ///Arabic - Lebanon
            ///</summary>
            ar_LB = 0x3001,

            ///<summary>
            ///Arabic - Libya
            ///</summary>
            ar_LY = 0x1001,

            ///<summary>
            ///Arabic - Morocco
            ///</summary>
            ar_MA = 0x1801,

            ///<summary>
            ///Arabic - Oman
            ///</summary>
            ar_OM = 0x2001,

            ///<summary>
            ///Arabic - Qatar
            ///</summary>
            ar_QA = 0x4001,

            ///<summary>
            ///Arabic - Saudi Arabia
            ///</summary>
            ar_SA = 0x0401,

            ///<summary>
            ///Arabic - Syria
            ///</summary>
            ar_SY = 0x2801,

            ///<summary>
            ///Arabic - Tunisia
            ///</summary>
            ar_TN = 0x1C01,

            ///<summary>
            ///Arabic - United Arab Emirates
            ///</summary>
            ar_AE = 0x3801,

            ///<summary>
            ///Arabic - Yemen
            ///</summary>
            ar_YE = 0x2401,

            ///<summary>
            ///Armenian
            ///</summary>
            hy = 0x002B,

            ///<summary>
            ///Armenian - Armenia
            ///</summary>
            hy_AM = 0x042B,

            ///<summary>
            ///Azeri
            ///</summary>
            az = 0x002C,

            ///<summary>
            ///Azeri (Cyrillic) - Azerbaijan
            ///</summary>
            az_AZ_Cyrl = 0x082C,

            ///<summary>
            ///Azeri (Latin) - Azerbaijan
            ///</summary>
            az_AZ_Latn = 0x042C,

            ///<summary>
            ///Basque
            ///</summary>
            eu = 0x002D,

            ///<summary>
            ///Basque - Basque
            ///</summary>
            eu_ES = 0x042D,

            ///<summary>
            ///Belarusian
            ///</summary>
            be = 0x0023,

            ///<summary>
            ///Belarusian - Belarus
            ///</summary>
            be_BY = 0x0423,

            ///<summary>
            ///Bulgarian
            ///</summary>
            bg = 0x0002,

            ///<summary>
            ///Bulgarian - Bulgaria
            ///</summary>
            bg_BG = 0x0402,

            ///<summary>
            ///Catalan
            ///</summary>
            ca = 0x0003,

            ///<summary>
            ///Catalan - Catalan
            ///</summary>
            ca_ES = 0x0403,

            ///<summary>
            ///Chinese - Hong Kong SAR
            ///</summary>
            zh_HK = 0x0C04,

            ///<summary>
            ///Chinese - Macau SAR
            ///</summary>
            zh_MO = 0x1404,

            ///<summary>
            ///Chinese - China
            ///</summary>
            zh_CN = 0x0804,

            ///<summary>
            ///Chinese (Simplified)
            ///</summary>
            zh_CHS = 0x0004,

            ///<summary>
            ///Chinese - Singapore
            ///</summary>
            zh_SG = 0x1004,

            ///<summary>
            ///Chinese - Taiwan
            ///</summary>
            zh_TW = 0x0404,

            ///<summary>
            ///Chinese (Traditional)
            ///</summary>
            zh_CHT = 0x7C04,

            ///<summary>
            ///Croatian
            ///</summary>
            hr = 0x001A,

            ///<summary>
            ///Croatian - Croatia
            ///</summary>
            hr_HR = 0x041A,

            ///<summary>
            ///Czech
            ///</summary>
            cs = 0x0005,

            ///<summary>
            ///Czech - Czech Republic
            ///</summary>
            cs_CZ = 0x0405,

            ///<summary>
            ///Danish
            ///</summary>
            da = 0x0006,

            ///<summary>
            ///Danish - Denmark
            ///</summary>
            da_DK = 0x0406,

            ///<summary>
            ///Dhivehi
            ///</summary>
            div = 0x0065,

            ///<summary>
            ///Dhivehi - Maldives
            ///</summary>
            div_MV = 0x0465,

            ///<summary>
            ///Dutch
            ///</summary>
            nl = 0x0013,

            ///<summary>
            ///Dutch - Belgium
            ///</summary>
            nl_BE = 0x0813,

            ///<summary>
            ///Dutch - The Netherlands
            ///</summary>
            nl_NL = 0x0413,

            ///<summary>
            ///English
            ///</summary>
            en = 0x0009,

            ///<summary>
            ///English - Australia
            ///</summary>
            en_AU = 0x0C09,

            ///<summary>
            ///English - Belize
            ///</summary>
            en_BZ = 0x2809,

            ///<summary>
            ///English - Canada
            ///</summary>
            en_CA = 0x1009,

            ///<summary>
            ///English - Caribbean
            ///</summary>
            en_CB = 0x2409,

            ///<summary>
            ///English - Ireland
            ///</summary>
            en_IE = 0x1809,

            ///<summary>
            ///English - Jamaica
            ///</summary>
            en_JM = 0x2009,

            ///<summary>
            ///English - New Zealand
            ///</summary>
            en_NZ = 0x1409,

            ///<summary>
            ///English - Philippines
            ///</summary>
            en_PH = 0x3409,

            ///<summary>
            ///English - South Africa
            ///</summary>
            en_ZA = 0x1C09,

            ///<summary>
            ///English - Trinidad and Tobago
            ///</summary>
            en_TT = 0x2C09,

            ///<summary>
            ///English - United Kingdom
            ///</summary>
            en_GB = 0x0809,

            ///<summary>
            ///English - United States
            ///</summary>
            en_US = 0x0409,

            ///<summary>
            ///English - Zimbabwe
            ///</summary>
            en_ZW = 0x3009,

            ///<summary>
            ///Estonian
            ///</summary>
            et = 0x0025,

            ///<summary>
            ///Estonian - Estonia
            ///</summary>
            et_EE = 0x0425,

            ///<summary>
            ///Faroese
            ///</summary>
            fo = 0x0038,

            ///<summary>
            ///Faroese - Faroe Islands
            ///</summary>
            fo_FO = 0x0438,

            ///<summary>
            ///Farsi
            ///</summary>
            fa = 0x0029,

            ///<summary>
            ///Farsi - Iran
            ///</summary>
            fa_IR = 0x0429,

            ///<summary>
            ///Finnish
            ///</summary>
            fi = 0x000B,

            ///<summary>
            ///Finnish - Finland
            ///</summary>
            fi_FI = 0x040B,

            ///<summary>
            ///French
            ///</summary>
            fr = 0x000C,

            ///<summary>
            ///French - Belgium
            ///</summary>
            fr_BE = 0x080C,

            ///<summary>
            ///French - Canada
            ///</summary>
            fr_CA = 0x0C0C,

            ///<summary>
            ///French - France
            ///</summary>
            fr_FR = 0x040C,

            ///<summary>
            ///French - Luxembourg
            ///</summary>
            fr_LU = 0x140C,

            ///<summary>
            ///French - Monaco
            ///</summary>
            fr_MC = 0x180C,

            ///<summary>
            ///French - Switzerland
            ///</summary>
            fr_CH = 0x100C,

            ///<summary>
            ///Galician
            ///</summary>
            gl = 0x0056,

            ///<summary>
            ///Galician - Galician
            ///</summary>
            gl_ES = 0x0456,

            ///<summary>
            ///Georgian
            ///</summary>
            ka = 0x0037,

            ///<summary>
            ///Georgian - Georgia
            ///</summary>
            ka_GE = 0x0437,

            ///<summary>
            ///German
            ///</summary>
            de = 0x0007,

            ///<summary>
            ///German - Austria
            ///</summary>
            de_AT = 0x0C07,

            ///<summary>
            ///German - Germany
            ///</summary>
            de_DE = 0x0407,

            ///<summary>
            ///German - Liechtenstein
            ///</summary>
            de_LI = 0x1407,

            ///<summary>
            ///German - Luxembourg
            ///</summary>
            de_LU = 0x1007,

            ///<summary>
            ///German - Switzerland
            ///</summary>
            de_CH = 0x0807,

            ///<summary>
            ///Greek
            ///</summary>
            el = 0x0008,

            ///<summary>
            ///Greek - Greece
            ///</summary>
            el_GR = 0x0408,

            ///<summary>
            ///Gujarati
            ///</summary>
            gu = 0x0047,

            ///<summary>
            ///Gujarati - India
            ///</summary>
            gu_IN = 0x0447,

            ///<summary>
            ///Hebrew
            ///</summary>
            he = 0x000D,

            ///<summary>
            ///Hebrew - Israel
            ///</summary>
            he_IL = 0x040D,

            ///<summary>
            ///Hindi
            ///</summary>
            hi = 0x0039,

            ///<summary>
            ///Hindi - India
            ///</summary>
            hi_IN = 0x0439,

            ///<summary>
            ///Hungarian
            ///</summary>
            hu = 0x000E,

            ///<summary>
            ///Hungarian - Hungary
            ///</summary>
            hu_HU = 0x040E,

            ///<summary>
            ///Icelandic.  Iceland uses reserved word 'is'
            ///</summary>
            ice = 0x000F,

            ///<summary>
            ///Icelandic - Iceland
            ///</summary>
            is_IS = 0x040F,

            ///<summary>
            ///Indonesian
            ///</summary>
            id = 0x0021,

            ///<summary>
            ///Indonesian - Indonesia
            ///</summary>
            id_ID = 0x0421,

            ///<summary>
            ///Italian
            ///</summary>
            it = 0x0010,

            ///<summary>
            ///Italian - Italy
            ///</summary>
            it_IT = 0x0410,

            ///<summary>
            ///Italian - Switzerland
            ///</summary>
            it_CH = 0x0810,

            ///<summary>
            ///Japanese
            ///</summary>
            ja = 0x0011,

            ///<summary>
            ///Japanese - Japan
            ///</summary>
            ja_JP = 0x0411,

            ///<summary>
            ///Kannada
            ///</summary>
            kn = 0x004B,

            ///<summary>
            ///Kannada - India
            ///</summary>
            kn_IN = 0x044B,

            ///<summary>
            ///Kazakh
            ///</summary>
            kk = 0x003F,

            ///<summary>
            ///Kazakh - Kazakhstan
            ///</summary>
            kk_KZ = 0x043F,

            ///<summary>
            ///Konkani
            ///</summary>
            kok = 0x0057,

            ///<summary>
            ///Konkani - India
            ///</summary>
            kok_IN = 0x0457,

            ///<summary>
            ///Korean
            ///</summary>
            ko = 0x0012,

            ///<summary>
            ///Korean - Korea
            ///</summary>
            ko_KR = 0x0412,

            ///<summary>
            ///Kyrgyz
            ///</summary>
            ky = 0x0040,

            ///<summary>
            ///Kyrgyz - Kazakhstan
            ///</summary>
            ky_KZ = 0x0440,

            ///<summary>
            ///Latvian
            ///</summary>
            lv = 0x0026,

            ///<summary>
            ///Latvian - Latvia
            ///</summary>
            lv_LV = 0x0426,

            ///<summary>
            ///Lithuanian
            ///</summary>
            lt = 0x0027,

            ///<summary>
            ///Lithuanian - Lithuania
            ///</summary>
            lt_LT = 0x0427,

            ///<summary>
            ///Macedonian
            ///</summary>
            mk = 0x002F,

            ///<summary>
            ///Macedonian - FYROM
            ///</summary>
            mk_MK = 0x042F,

            ///<summary>
            ///Malay
            ///</summary>
            ms = 0x003E,

            ///<summary>
            ///Malay - Brunei
            ///</summary>
            ms_BN = 0x083E,

            ///<summary>
            ///Malay - Malaysia
            ///</summary>
            ms_MY = 0x043E,

            ///<summary>
            ///Marathi
            ///</summary>
            mr = 0x004E,

            ///<summary>
            ///Marathi - India
            ///</summary>
            mr_IN = 0x044E,

            ///<summary>
            ///Mongolian
            ///</summary>
            mn = 0x0050,

            ///<summary>
            ///Mongolian - Mongolia
            ///</summary>
            mn_MN = 0x0450,

            ///<summary>
            ///Norwegian
            ///</summary>
            no = 0x0014,

            ///<summary>
            ///Norwegian (Bokml) - Norway
            ///</summary>
            nb_NO = 0x0414,

            ///<summary>
            ///Norwegian (Nynorsk) - Norway
            ///</summary>
            nn_NO = 0x0814,

            ///<summary>
            ///Polish
            ///</summary>
            pl = 0x0015,

            ///<summary>
            ///Polish - Poland
            ///</summary>
            pl_PL = 0x0415,

            ///<summary>
            ///Portuguese
            ///</summary>
            pt = 0x0016,

            ///<summary>
            ///Portuguese - Brazil
            ///</summary>
            pt_BR = 0x0416,

            ///<summary>
            ///Portuguese - Portugal
            ///</summary>
            pt_PT = 0x0816,

            ///<summary>
            ///Punjabi
            ///</summary>
            pa = 0x0046,

            ///<summary>
            ///Punjabi - India
            ///</summary>
            pa_IN = 0x0446,

            ///<summary>
            ///Romanian
            ///</summary>
            ro = 0x0018,

            ///<summary>
            ///Romanian - Romania
            ///</summary>
            ro_RO = 0x0418,

            ///<summary>
            ///Russian
            ///</summary>
            ru = 0x0019,

            ///<summary>
            ///Russian - Russia
            ///</summary>
            ru_RU = 0x0419,

            ///<summary>
            ///Sanskrit
            ///</summary>
            sa = 0x004F,

            ///<summary>
            ///Sanskrit - India
            ///</summary>
            sa_IN = 0x044F,

            ///<summary>
            ///Serbian (Cyrillic) - Serbia
            ///</summary>
            sr_SP_Cyrl = 0x0C1A,

            ///<summary>
            ///Serbian (Latin) - Serbia
            ///</summary>
            sr_SP_Latn = 0x081A,

            ///<summary>
            ///Slovak
            ///</summary>
            sk = 0x001B,

            ///<summary>
            ///Slovak - Slovakia
            ///</summary>
            sk_SK = 0x041B,

            ///<summary>
            ///Slovenian
            ///</summary>
            sl = 0x0024,

            ///<summary>
            ///Slovenian - Slovenia
            ///</summary>
            sl_SI = 0x0424,

            ///<summary>
            ///Spanish
            ///</summary>
            es = 0x000A,

            ///<summary>
            ///Spanish - Argentina
            ///</summary>
            es_AR = 0x2C0A,

            ///<summary>
            ///Spanish - Bolivia
            ///</summary>
            es_BO = 0x400A,

            ///<summary>
            ///Spanish - Chile
            ///</summary>
            es_CL = 0x340A,

            ///<summary>
            ///Spanish - Colombia
            ///</summary>
            es_CO = 0x240A,

            ///<summary>
            ///Spanish - Costa Rica
            ///</summary>
            es_CR = 0x140A,

            ///<summary>
            ///Spanish - Dominican Republic
            ///</summary>
            es_DO = 0x1C0A,

            ///<summary>
            ///Spanish - Ecuador
            ///</summary>
            es_EC = 0x300A,

            ///<summary>
            ///Spanish - El Salvador
            ///</summary>
            es_SV = 0x440A,

            ///<summary>
            ///Spanish - Guatemala
            ///</summary>
            es_GT = 0x100A,

            ///<summary>
            ///Spanish - Honduras
            ///</summary>
            es_HN = 0x480A,

            ///<summary>
            ///Spanish - Mexico
            ///</summary>
            es_MX = 0x080A,

            ///<summary>
            ///Spanish - Nicaragua
            ///</summary>
            es_NI = 0x4C0A,

            ///<summary>
            ///Spanish - Panama
            ///</summary>
            es_PA = 0x180A,

            ///<summary>
            ///Spanish - Paraguay
            ///</summary>
            es_PY = 0x3C0A,

            ///<summary>
            ///Spanish - Peru
            ///</summary>
            es_PE = 0x280A,

            ///<summary>
            ///Spanish - Puerto Rico
            ///</summary>
            es_PR = 0x500A,

            ///<summary>
            ///Spanish - Spain
            ///</summary>
            es_ES = 0x0C0A,

            ///<summary>
            ///Spanish - Uruguay
            ///</summary>
            es_UY = 0x380A,

            ///<summary>
            ///Spanish - Venezuela
            ///</summary>
            es_VE = 0x200A,

            ///<summary>
            ///Swahili
            ///</summary>
            sw = 0x0041,

            ///<summary>
            ///Swahili - Kenya
            ///</summary>
            sw_KE = 0x0441,

            ///<summary>
            ///Swedish
            ///</summary>
            sv = 0x001D,

            ///<summary>
            ///Swedish - Finland
            ///</summary>
            sv_FI = 0x081D,

            ///<summary>
            ///Swedish - Sweden
            ///</summary>
            sv_SE = 0x041D,

            ///<summary>
            ///Syriac
            ///</summary>
            syr = 0x005A,

            ///<summary>
            ///Syriac - Syria
            ///</summary>
            syr_SY = 0x045A,

            ///<summary>
            ///Tamil
            ///</summary>
            ta = 0x0049,

            ///<summary>
            ///Tamil - India
            ///</summary>
            ta_IN = 0x0449,

            ///<summary>
            ///Tatar
            ///</summary>
            tt = 0x0044,

            ///<summary>
            ///Tatar - Russia
            ///</summary>
            tt_RU = 0x0444,

            ///<summary>
            ///Telugu
            ///</summary>
            te = 0x004A,

            ///<summary>
            ///Telugu - India
            ///</summary>
            te_IN = 0x044A,

            ///<summary>
            ///Thai
            ///</summary>
            th = 0x001E,

            ///<summary>
            ///Thai - Thailand
            ///</summary>
            th_TH = 0x041E,

            ///<summary>
            ///Turkish
            ///</summary>
            tr = 0x001F,

            ///<summary>
            ///Turkish - Turkey
            ///</summary>
            tr_TR = 0x041F,

            ///<summary>
            ///Ukrainian
            ///</summary>
            uk = 0x0022,

            ///<summary>
            ///Ukrainian - Ukraine
            ///</summary>
            uk_UA = 0x0422,

            ///<summary>
            ///Urdu
            ///</summary>
            ur = 0x0020,

            ///<summary>
            ///Urdu - Pakistan
            ///</summary>
            ur_PK = 0x0420,

            ///<summary>
            ///Uzbek
            ///</summary>
            uz = 0x0043,

            ///<summary>
            ///Uzbek (Cyrillic) - Uzbekistan
            ///</summary>
            uz_UZ_Cyrl = 0x0843,

            ///<summary>
            ///Uzbek (Latin) - Uzbekistan
            ///</summary>
            uz_UZ_Latn = 0x0443,

            ///<summary>
            ///Vietnamese
            ///</summary>
            vi = 0x002A,


            ///<summary>
            ///Vietnamese - Vietnam
            ///</summary>
            vi_VN = 0x042A,
    }
}
