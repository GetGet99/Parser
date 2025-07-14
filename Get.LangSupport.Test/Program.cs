// See https://aka.ms/new-console-template for more information

using Get.LangSupport;
using Get.LangSupport.Test;

var metadata = new TextmateGrammarMetadata
{
    LanguageId = "testlang",
    LanguageExtensions = [".testlang"]
};

//string contrib = metadata.GetContributionsJSON();
string grammar = metadata.GetGrammarJSON(TextmateGrammarGenerator.GenerateRepository<CustomLexerSourceGen>());

Console.WriteLine(grammar);
