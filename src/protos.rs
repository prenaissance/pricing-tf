pub mod pricing_tf {
    pub mod pricing {
        pub mod v1 {
            tonic::include_proto!("pricing_tf.pricing.v1");
        }
    }
    pub mod blocked_users {
        pub mod v1 {
            tonic::include_proto!("pricing_tf.blocked_users.v1");
        }
    }
}
