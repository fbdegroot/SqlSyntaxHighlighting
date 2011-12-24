namespace SqlSyntaxHighlighting.NaturalTextTaggers
{
    enum State
    {
        Default,             // default start state.

        String,              // string ("...")
        MultiLineString,     // multi-line string (@"...")
    }
}
