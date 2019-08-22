#from pygments.lexers.asm import CppLexer
from pygments.lexers.dotnet import CSharpLexer
from pygments.token import Name, Keyword

class MDCSharpLexer(CSharpLexer):
    name = 'MDCSharp'
    aliases = ['mdcsharp']

    info = [('MetaData',Name,Keyword.Type)]


    def get_tokens_unprocessed(self, text):
        for index, token, value in CSharpLexer.get_tokens_unprocessed(self, text):
            submitted = False
            for (s,a,t) in self.info:
                if token is a and value == s:
                    submitted = True
                    yield index, t, value
                    break
            if(submitted == False):
                yield index, token, value