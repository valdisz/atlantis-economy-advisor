namespace advisor.Persistence {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using HotChocolate;

    public class DbStudyPlan : InTurnContext {
        public int UnitNumber { get; set; }

        [GraphQLIgnore]
        public int TurnNumber { get; set; }

        [GraphQLIgnore]
        public long PlayerId { get; set; }

        public DbSkill Target { get; set; }

        // TODO: Use size constant
        [MaxLength(64)]
        public string Study { get; set; }

        public List<int> Teach { get; set; } = new ();

        [GraphQLIgnore]
        public DbPlayerTurn Turn { get; set; }

        [GraphQLIgnore]
        public DbUnit Unit { get; set; }
    }
}
