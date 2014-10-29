using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSpec;

namespace ClassLibrary2
{
    class describe_some_class : nspec
    {
        void given_true_is_true()
        {
            it["true is true"] = () => true.should_be_true();
        }
    }
}
