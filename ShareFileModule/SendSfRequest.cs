using ShareFile.Api.Client.Exceptions;
using ShareFile.Api.Client.Models;
using ShareFile.Api.Client.Requests;
using ShareFile.Api.Client.Requests.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace ShareFile.Api.Powershell
{
    [Cmdlet(VerbsCommunications.Send, Noun)]
    public class SendSfRequest : BaseCmdlet
    {
        private const string Noun = "SfRequest";

        [Parameter(
            Position = 0,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public PSShareFileClient Client { get; set; }

        [Parameter(Position = 1)]
        public string Method { get; set; }

        [Parameter(Position = 2)]
        public Uri Uri { get; set; }

        [Parameter(Mandatory = false, Position = 3, ValueFromPipeline = true)]
        public ODataObject Body { get; set; }

        [Parameter]
        public string Entity { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string Navigation { get; set; }

        [Parameter]
        public string Action { get; set; }

        [Parameter]
        public string Cast { get; set; }

        [Parameter]
        public System.Collections.Hashtable Parameters { get; set; }

        [Parameter]
        public string Expand { get; set; }

        [Parameter]
        public string Select { get; set; }

        [Parameter]
        public string Filter { get; set; }

        [Parameter]
        public string BodyText { get; set; }


        [Parameter]
        public string Account { get; set; }

        [Parameter]
        public bool Redirect { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        protected override void ProcessRecord()
        {
            if (Id != null && Uri != null) throw new Exception("Set only Id or Uri");
            if (Action == null && Cast != null) Action = Cast;
            if (Action == null && Navigation != null) Action = Navigation;
            if (Method == null) Method = "GET";
            Method = Method.ToUpper();

            if (Redirect)
            {
                if (Credential != null)
                {
                    Client.Client.AddCredentials(Uri, "Basic", Credential.GetNetworkCredential());
                }
                else
                {
                    throw new Exception("Parameter 'Credential' missing for a redirection uri.");
                }
            }

            Query<ODataObject> query = new Query<ODataObject>(Client.Client);
            query.HttpMethod = Method;

            if (string.IsNullOrWhiteSpace(Filter) == false) query.Filter(AddFilter());
            if (Entity != null) query = query.From(Entity);
            if (Action != null) query = query.Action(Action);
            if (Id != null) query = query.Id(Id);
            else if (Uri != null) query = query.Id(Uri.ToString());

            if (Parameters != null)
            {
                foreach (var key in Parameters.Keys)
                {
                    if (!(key is string)) throw new Exception("Use strings for parameter keys");
                    query = query.QueryString((string)key, Parameters[key].ToString());
                }
            }
            if (Expand != null) query = query.Expand(Expand);
            if (Select != null) query = query.Select(Select);
            if (Body != null)
            {
                query.Body = Body;
            }
            else if (BodyText != null)
            {
                query.Body = BodyText;
            }
            try
            {
                var response = query.Execute();
                if (response != null)
                {
                    Type t = response.GetType();
                    if (t.IsGenericType)
                    {
                        if (t.GetGenericTypeDefinition() == typeof(ODataFeed<>))
                        {
                            var feed = t.GetProperty("Feed").GetValue(response, null) as IEnumerable<ODataObject>;
                            foreach (var o in feed)
                            {
                                WriteObject(o);
                            }
                        }
                    }
                    else
                    {
                        WriteObject(response);
                    }
                }
            }
            catch (ODataException e)
            {
                WriteError(new ErrorRecord(new Exception(e.Code.ToString() + ": " + e.ODataExceptionMessage.Message), e.Code.ToString(), ErrorCategory.NotSpecified, query.GetEntity()));
            }
            catch (Exception)
            {
                ShowSuggestion(Entity, Action, Parameters);
                throw;
            }
        }


        
        private IFilter AddFilter()
        {
            FilterBuilder builder = new FilterBuilder(Filter);
            return builder.Build();
        }

        /// <summary>
        /// This class to be used to create the filter objects from string
        /// </summary>
        private class FilterBuilder
        {
            private enum OperatorType
            {
                none,
                eq,// Equal to 
                ne,// Not Equal to
                gt,// Greater than
                ge,// Greater than or Equal to
                lt,// Less than
                le,// Less than or Equal to
                and,// logical And operator
                or,// logical Or operator
                not,// logical Not operator
                add,// arithmetic ADDITION operator
                sub,// arithmetic SUBTRACTION operator
                mul,// arithmetic Multiplication operator
                div,// arithmetic Division operator
                mod,// arithmetic Modulo operator
                startswith,// string Starts With operator
                endswith,// string Ends With operator
                substringof,// string Sub String operator
                precedence // Precedence grouping (parenthesis)
            }

            private Stack<string> OperatorsStack;
            private Stack<string> OperandsStack;
            private string FilterBody;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="filterBody">Filter parameter text</param>
            public FilterBuilder(string filterBody)
            {
                OperatorsStack = new Stack<string>();
                OperandsStack = new Stack<string>();
                FilterBody = filterBody;
            }

            /// <summary>
            /// Parse the filter body text and created Filter object
            /// </summary>
            /// <returns>Return a composite IFilter object</returns>
            public IFilter Build()
            {
                SplitText(FilterBody);
                return GetFilter();
            }

            /// <summary>
            /// Parse and split the text into filters and store them into stacks of Filter operands(properties) and operators
            /// It will match the text with different set of operator types, and then split text accordingly
            /// </summary>
            /// <param name="filterText">Filter body text</param>
            private void SplitText(string filterText)
            {
                string[] result = null;

                string propertyName = string.Empty;
                string filterOperator = string.Empty;
                string propertyValue = string.Empty;

                // first match if text starts with any logical (and, or, not) operator
                Match match = CheckIfLogicalOperation(filterText);
                if (match.Success)
                {
                    // if expression matches then add the logical operator in OperatorsStack and check & split rest of text recursively
                    OperatorsStack.Push(match.Value.Trim());
                    filterText = filterText.Substring(match.Length).Trim();
                    SplitText(filterText);
                }
                else
                {
                    // if it is some string operation (endswith, startswith)
                    match = CheckIfStringOperation(filterText);
                    if (match.Success)
                    {
                        // if expression matches then split the operands and operator and add them in stacks
                        string[] sepBraces = { "(", ",", ")" };
                        result = filterText.Split(sepBraces, 4, StringSplitOptions.RemoveEmptyEntries);

                        filterOperator = result[0].Trim();
                        propertyName = result[1].Trim();
                        propertyValue = result[2].Trim();

                        // perform the split operation on rest of text if there is any
                        if (result.Length == 4)
                        {
                            SplitText(result[3].Trim());
                        }
                    }
                    else
                    {
                        // if it is some relational/comparision operator (eq,ne,lt,le,gt,ge)
                        match = CheckIfRelationalOperation(filterText);
                        if (match.Success)
                        {
                            // if expression matches then split the operands and operator and add them in stacks
                            // following are regular expression to split all three parts i.e. Property name, operator and property value
                            Regex expLeft = new Regex(@"^\w+\b");
                            Regex expOperator = new Regex(@"^((eq|ne|lt|le|gt|ge)\b|(=|==|!=|<>|<|<=|>|>=))");
                            Regex expRight = new Regex(@"^(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)");

                            match = expLeft.Match(filterText);
                            propertyName = match.Value.Trim();
                            filterText = filterText.Substring(match.Length).Trim();

                            match = expOperator.Match(filterText);
                            filterOperator = match.Value.Trim();
                            filterText = filterText.Substring(match.Length).Trim();

                            match = expRight.Match(filterText);
                            propertyValue = match.Value.Trim();
                            filterText = filterText.Substring(match.Length).Trim();

                            // perform the split operation on rest of text
                            if (!string.IsNullOrWhiteSpace(filterText))
                            {
                                SplitText(filterText);
                            }
                        }
                        else
                        {
                            // if it is some arithmetic operation (add,sub,div,mul,mod)
                            match = CheckIfArithmeticOperation(filterText);
                            if (match.Success)
                            {
                                // if expression matches then split the operands and operator and add them in stacks
                                // following are regular expression to split all five parts i.e. Property name, operator and then sub operation details i.e. two operands values and a relational operator
                                Regex expLeft = new Regex(@"^\w+\b");
                                Regex expOperator = new Regex(@"^(add|sub|mul|div|mod)\b");
                                Regex innerExpLeft = new Regex(@"^(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)");
                                Regex innerExpOperator = new Regex(@"^(((eq|ne|lt|le|gt|ge)\s+)|(\s*(=|==|!=|<>|<|<=|>|>=)\s*))\b");
                                Regex innerExpRight = new Regex(@"^(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)");

                                match = expLeft.Match(filterText);
                                propertyName = match.Value.Trim();
                                filterText = filterText.Substring(match.Length).Trim();

                                match = expOperator.Match(filterText);
                                filterOperator = match.Value.Trim();
                                filterText = filterText.Substring(match.Length).Trim();

                                match = innerExpLeft.Match(filterText);
                                propertyValue = match.Value.Trim();
                                filterText = filterText.Substring(match.Length).Trim();

                                match = innerExpOperator.Match(filterText);
                                string innerOperator = match.Value.Trim();
                                filterText = filterText.Substring(match.Length).Trim();

                                match = innerExpRight.Match(filterText);
                                string propertyValue2 = match.Value.Trim();
                                filterText = filterText.Substring(match.Length).Trim();

                                // perform the split operation on rest of text
                                if (!string.IsNullOrWhiteSpace(filterText))
                                {
                                    SplitText(filterText);
                                }

                                // adding operands(properties & values) and operators in their respective stack
                                OperatorsStack.Push(innerOperator);
                                OperandsStack.Push(RemoveQuotes(propertyValue2));
                            }
                            else
                            {
                                throw new Exception("Invalid filter, please check syntax and try again.");
                            }
                        }
                    }

                    // adding operands(properties & values) and operators in their respective stack
                    OperatorsStack.Push(filterOperator);
                    OperandsStack.Push(propertyName);
                    OperandsStack.Push(RemoveQuotes(propertyValue));
                }
            }


            /// <summary>
            /// remove quotes around the text, as the quotes will be added in the filter classes on property values
            /// </summary>
            private string RemoveQuotes(string value)
            {
                return value.Trim("'\"".ToCharArray());
            }

            /// <summary>
            /// Logical operator match. Returns true if text starts with 'and', 'or' or 'not' prefix
            /// </summary>
            private Match CheckIfLogicalOperation(string filterText)
            {
                Regex exp = new Regex(@"^(and|or|not)\s");
                return exp.Match(filterText);
            }

            /// <summary>
            /// Relational operator match. Returns true if expression matches e.g. Name eq 'Neil Johnson' or Name='John'
            /// </summary>
            private Match CheckIfRelationalOperation(string filterText)
            {
                Regex exp = new Regex(@"^\w+((\s+(eq|ne|lt|le|gt|ge)\s+)|(\s*(=|==|!=|<>|<|<=|>|>=)\s*))(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)");
                return exp.Match(filterText);
            }

            /// <summary>
            /// String operator match. Returns true if expression matches e.g. startwith(Name, 'Neil')
            /// </summary>
            private Match CheckIfStringOperation(string filterText)
            {
                Regex exp = new Regex(@"^(endswith|startswith|substr|substring|substringof)\(\w+\s*\,\s*(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)\)");
                return exp.Match(filterText);
            }

            /// <summary>
            /// Arithmetic operator match. Returns true if expression matches e.g. Size add 250 gt 500
            /// </summary>
            private Match CheckIfArithmeticOperation(string filterText)
            {
                Regex exp = new Regex(@"^\w+\s+(add|sub|mul|div|mod)\s+(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)((\s+(eq|ne|lt|le|gt|ge)\s+)|(\s*(=|==|!=|<>|<|<=|>|>=)\s*))(?:"".*?""|'.*?'|[a-zA-Z0-9.]+)");
                return exp.Match(filterText);
            }

            /// <summary>
            /// Generate the IFiler object from stack of Operators and Operands.
            /// The values in both stacks are in order, it will pick one by one operator from stack 
            /// and depending on operator type it will fetch the property names and values (operands) to generate the Filter
            /// </summary>
           /// <returns></returns>
           private IFilter GetFilter()
           {
                // Keep track of all the IFilter instances, if two Filters are added in it then next operator must be 'and' or 'or' operation to generate one expression
                Stack<IFilter> operations = new Stack<IFilter>();
                IFilter filterType = null;

                while (OperatorsStack.Count > 0)
                {
                    string propertyName = null;
                    string propertyValue = null;
                    IFilter left = null;
                    IFilter right = null;
                    string op = OperatorsStack.Pop();
                    OperatorType operators = GetOperator(op);

                    switch (operators)
                    {
                        case OperatorType.eq:
                        case OperatorType.ne:
                        case OperatorType.startswith:
                        case OperatorType.endswith:
                        case OperatorType.substringof:
                        case OperatorType.lt:
                        case OperatorType.le:
                        case OperatorType.gt:
                        case OperatorType.ge:
                            propertyValue = OperandsStack.Pop();
                            propertyName = OperandsStack.Pop();

                            filterType = CreateFilter(operators, propertyName, propertyValue);
                            break;
                        /* currently not supported
                        case OperatorType.add:
                        case OperatorType.sub:
                        case OperatorType.mul:
                        case OperatorType.div:
                        case OperatorType.mod:
                            propertyValue = OperandsStack.Pop();
                            propertyName = OperandsStack.Pop();
                            string propertyValue2 = OperandsStack.Pop();
                            OperatorType operatorInner = GetOperator(OperatorsStack.Pop());

                            filterType = CreateFilter(operators, propertyName, propertyValue, operatorInner, propertyValue2);
                            break;
                        */
                        case OperatorType.and:
                            right = operations.Pop();
                            left = operations.Pop();
                            filterType = new AndFilter(left, right);
                            break;

                        case OperatorType.or:
                            right = operations.Pop();
                            left = operations.Pop();
                            filterType = new OrFilter(left, right);
                            break;
                        /* currently not supported
                        case OperatorType.not:
                            right = operations.Pop();
                            filterType = new NotFilter(right);
                            break;
                         */
                    }

                    operations.Push(filterType);
                }

                return operations.Pop();
            }

            private OperatorType GetOperator(string op)
            {
                if (Enum.IsDefined(typeof(OperatorType), op))
                {
                    return (OperatorType)Enum.Parse(typeof(OperatorType), op, true);
                }

                switch (op)
                {
                    case "=":
                    case "==":
                        return OperatorType.eq;
                    case "!=":
                    case "<>":
                        return OperatorType.ne;
                    case "<":
                        return OperatorType.le;
                    case "<=":
                        return OperatorType.lt;
                    case ">":
                        return OperatorType.gt;
                    case ">=":
                        return OperatorType.ge;
                    case "substr":
                    case "substring":
                        return OperatorType.substringof;
                    default:
                        return OperatorType.none;
                }
            }

            private IFilter CreateFilter(OperatorType operators, string propertyName, string propertyValue)
            {
                IFilter filter = null;
                switch (operators)
                {
                    case OperatorType.eq:
                        filter = new EqualToFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.ne:
                        filter = new NotEqualToFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.startswith:
                        filter = new StartsWithFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.endswith:
                        filter = new EndsWithFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.substringof:
                        filter = new SubstringFilter(propertyName, propertyValue);
                        break;
                    /* currently not supported
                    case OperatorType.lt:
                        filter = new LessThanFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.le:
                        filter = new LessThanOrEqualFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.gt:
                        filter = new GreaterThanFilter(propertyName, propertyValue);
                        break;
                    case OperatorType.ge:
                        filter = new GreaterThanOrEqualFilter(propertyName, propertyValue);
                        break;
                     */
                }

                return filter;
            }
            /*
            private IFilter CreateFilter(OperatorType operators, string propertyName, string propertyValue1, OperatorType innerOperator, string propertyValue2)
            {
                IFilter filter = null;
                switch (operators)
                {
                    case OperatorType.add:
                        filter = new AdditionFilter(propertyName, propertyValue1, innerOperator.ToString(), propertyValue2);
                        break;
                    case OperatorType.sub:
                        filter = new SubtractionFilter(propertyName, propertyValue1, innerOperator.ToString(), propertyValue2);
                        break;
                    case OperatorType.mul:
                        filter = new MultiplicationFilter(propertyName, propertyValue1, innerOperator.ToString(), propertyValue2);
                        break;
                    case OperatorType.div:
                        filter = new DivisionFilter(propertyName, propertyValue1, innerOperator.ToString(), propertyValue2);
                        break;
                    case OperatorType.mod:
                        filter = new ModuloFilter(propertyName, propertyValue1, innerOperator.ToString(), propertyValue2);
                        break;
                }

                return filter;
            }
             */
        }
     
 
        private void ShowSuggestion(string Entity, string Action, Hashtable parameters)
        {
            if("Items".Equals(Entity,StringComparison.CurrentCultureIgnoreCase) &&
               "Download".Equals(Action,StringComparison.CurrentCultureIgnoreCase) && 
               IsMissingParam("redirect",parameters))

            {                
                WriteWarning("You may want to append:    -Parameters @{'redirect' = \"false\"}    for the command to work correctly.\n\n");
                return;
            }          
  
            ///Add more suggestions here.
        }

        private bool IsMissingParam(string param, Hashtable parameters)
        {
             if(parameters == null || param == null)
             {
                 return true;
             }

             foreach (DictionaryEntry entry in parameters)
             {
                 if(entry.Key.Equals(param))
                 {
                     return false;
                 }
             }

             return true;
        }
    }
}
