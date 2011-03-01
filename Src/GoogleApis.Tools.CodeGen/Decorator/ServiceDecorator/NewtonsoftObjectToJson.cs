using System;
using System.CodeDom;

using Newtonsoft.Json;

using Google.Apis.Testing;

namespace Google.Apis.Tools.CodeGen.Decorator.ServiceDecorator
{
    /// <summary>
    /// Supplies ObjectToJson method in generated services. Using Newtonsoft.Json.JsonSerilizer
    /// </summary>
    public class NewtonsoftObjectToJson : IServiceDecorator
    {
        private const string FieldName = "newtonJsonSerilizer";
        private const string PropertyName = "NewtonJsonSerilizer";
        private const string MethodName = "ObjectToJson";
        private const string settingsVarName = "settings";
        
        /// <summary>
        /// Creates a JsonSerializer field.
        /// <code>private JsonSerializer newtonJsonSerilizer = null;</code>
        /// </summary>
        [VisibleForTestOnly]
        internal CodeMemberField CreateJsonSerializerField()
        {
            var field = new CodeMemberField(typeof(JsonSerializer), FieldName);
            field.InitExpression = new CodePrimitiveExpression(null);
            field.Attributes = MemberAttributes.Private;
            return field;
        }
        
        [VisibleForTestOnly]
        /// <summary>
        /// Creates a property to configure and create JsonSerilizer 
        /// <code>
        ///     private JsonSerializer NewtonJsonSerilizer
        ///     {
        ///         get
        ///         {
        ///             if( this.newtonwJsonSerilizer == null )
        ///             {
        ///                 JsonSerializerSettings settings = new JsonSerializerSettings();
        ///                 settings.NullValueHandling = NullValueHandling.Ignore;
        ///                 this.newtonwJsonSerilizer = JsonSerializer.Create(settings);            
        ///             }
        ///             return this.newtonwJsonSerilizer;
        ///         }
        ///     }
        /// </code>
        /// </summary>
        /// <returns>
        /// A <see cref="CodeMemberProperty"/>
        /// </returns>
        internal CodeMemberProperty CreateJsonSerializerGetter()
        {
            var property = new CodeMemberProperty();
            
            //private JsonSerializer NewtonJsonSerilizer
            property.Name = PropertyName;
            property.Attributes = MemberAttributes.Private;
            property.HasSet = false;
            property.HasGet = true;
            
            var creationBlock = CreateSerilizerCreationBlock ();
            
            // if( this.newtonwJsonSerilizer == null )
            //      ... // creationBlock
            var condition = new CodeBinaryOperatorExpression();
            condition.Operator = CodeBinaryOperatorType.IdentityEquality;
            condition.Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), FieldName);
            condition.Right = new CodePrimitiveExpression(null);
            
            var ifStatement = new CodeConditionStatement();
            ifStatement.Condition = condition;
            ifStatement.TrueStatements.AddRange(creationBlock);
            
            // return this.newtonwJsonSerilizer;
            var returnStatment = new CodeMethodReturnStatement();
            returnStatment.Expression = new CodeFieldReferenceExpression(
                                            new CodeThisReferenceExpression(),
                                            FieldName);
            
            property.GetStatements.Add(ifStatement);
            property.GetStatements.Add(returnStatment);
            return property;
        }
        
        /// <summary>
        /// Creates the following block of code
        ///  <code>
        ///     JsonSerializerSettings settings = new JsonSerializerSettings();
        ///     settings.NullValueHandling = NullValueHandling.Ignore;
        ///     this.newtonwJsonSerilizer = JsonSerializer.Create(settings);
        ///  </code>
        /// </summary>
        /// <returns>
        /// A <see cref="CodeStatementCollection"/>
        /// </returns>
        private CodeStatementCollection CreateSerilizerCreationBlock ()
        {
            // JsonSerializerSettings settings = new JsonSerializerSettings()
            var settingsDeclarAndConstruct = new CodeVariableDeclarationStatement(
                    typeof(JsonSerializerSettings),
                    settingsVarName,
                    new CodeObjectCreateExpression(typeof(JsonSerializerSettings)));
            
            // settings.NullValueHandling = NullValueHandling.Ignore;
            var assignNullValueHandling = 
                new CodeAssignStatement(
                    new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression(settingsVarName), "NullValueHandling"),
                    new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(NullValueHandling)),
                        "Ignore"));
            
            // this.newtonwJsonSerilizer = JsonSerializer.Create(settings);
            var createSerilizerFromSettings = 
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), FieldName),
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(JsonSerializer)),
                        "Create",
                        new CodeVariableReferenceExpression(settingsVarName)));
            
            var creationBlock = new CodeStatementCollection();            
            creationBlock.Add(settingsDeclarAndConstruct);
            creationBlock.Add(assignNullValueHandling);
            creationBlock.Add(createSerilizerFromSettings);
            return creationBlock;
        }
        
        /// <summary>
        ///  Creates ObjectToJson method
        ///  <code>
        ///     public string ObjectToJson(object obj)
        ///     {
        ///         TextWriter tw = new StringWriter();
        ///         NewtonJsonSerilizer.Serialize(tw, obj);
        ///         return tw.ToString()
        ///     }
        ///  </code>
        /// </summary>
        [VisibleForTestOnly]
        internal CodeMemberMethod CreateObjectToJson()
        {
            // public string ObjectToJson(object obj)
            var method = new CodeMemberMethod();
            method.Name = MethodName;
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "obj"));
            method.ReturnType = new CodeTypeReference(typeof(string));
            
            // TextWriter tw = new StringWriter();
            var textWriterDeclaration = new CodeVariableDeclarationStatement();
            textWriterDeclaration.Name = "tw";
            textWriterDeclaration.Type = new CodeTypeReference(typeof(System.IO.TextWriter));
            textWriterDeclaration.InitExpression = new CodeObjectCreateExpression(typeof(System.IO.TextWriter));
            
            // NewtonJsonSerilizer.Serialize(tw, obj);
            var serializeCall = new CodeMethodInvokeExpression();
            serializeCall.Parameters.Add(new CodeVariableReferenceExpression("tw"));
            serializeCall.Parameters.Add(new CodeVariableReferenceExpression("obj"));
            serializeCall.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Serialize");
            
            //return tw.ToString()
            var returnStatment = new CodeMethodReturnStatement();
            returnStatment.Expression = new CodeMethodInvokeExpression(
                                           new CodeVariableReferenceExpression("tw"),
                                           "ToString");
           
            method.Statements.Add(textWriterDeclaration);
            method.Statements.Add(serializeCall);
            method.Statements.Add(returnStatment);
            
            return method;
        }
        
        public void DecorateClass (Discovery.IService service, CodeTypeDeclaration serviceClass)
        {
            serviceClass.Members.Add(CreateJsonSerializerField());
            serviceClass.Members.Add(CreateJsonSerializerGetter());
            serviceClass.Members.Add(CreateObjectToJson());
        }        
    }
}

