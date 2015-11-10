using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using ScrimpNet.Reflection;

namespace ScrimpNet.Security.Core
{
    public class SecurityUtils
    {
        /// <summary>
        /// Get a list of questions which can be used in 'Forgot Password' scenarios.  Questions are
        /// sourced from SecurityQuestion.xml.  Each call will return random number of questions in random order
        /// </summary>
        /// <param name="groupKeys">Any one or combination of 'Good', 'Fair', 'Poor'.  
        /// Questions are randomly picked from any of the provided categories.  groupKey is ignored if value is not found.  If no groupKeys are found
        /// then only 'Good' keys (with less questions available) are returned</param>
        /// <param name="minQuestions">The least number of qsuestions to return. (inclusive)</param>
        /// <param name="maxQuestions">Maximum number of questions to return (inclusive)</param>
        /// <returns>List of questions</returns>
        public static List<string> GetSecurityQuestions(string[] groupKeys, int minQuestions, int maxQuestions)
        {
            string questionListText = Resource.EmbeddedFile(MethodInfo.GetCurrentMethod().Module.Assembly ,"ScrimpNet.Security.Core.SecurityQuestions.xml");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(questionListText);

            SortedDictionary<int, string> internalQuestionList = new SortedDictionary<int, string>();
            Random rdm = new Random();

            foreach (string groupKey in groupKeys)
            {
                string key = groupKey;
                if (string.IsNullOrWhiteSpace(key) == true)
                {
                    key = "";
                }
                key = groupKey.ToLower();
                string query = string.Format("/questionList/question[group='{0}']/text", key);
                XmlNodeList allNodes = doc.SelectNodes(query);
                foreach (XmlNode  node in allNodes)
                {
                    internalQuestionList.Add(rdm.Next(int.MinValue, int.MaxValue), node.InnerText); //puts question in random order
                }
                if (internalQuestionList.Count >= maxQuestions) break;
            }

            int totalQuestionsToReturn = rdm.Next(minQuestions, Math.Min(internalQuestionList.Count, maxQuestions));

            var x = from kvpQuestion in internalQuestionList.Take(totalQuestionsToReturn)
                    select kvpQuestion.Value;
            if (x == null) return new List<string>(); // no questions found. Probably due to bad groupKey
            return x.ToList();
        }
    }
}
