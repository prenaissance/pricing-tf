use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, Clone, Copy)]
pub struct Tf2Currency {
    #[serde(default)]
    pub metal: f64,
    #[serde(default)]
    pub keys: f64,
}

impl Tf2Currency {
    pub fn new(keys: f64, metal: f64) -> Self {
        Self { keys, metal }
    }

    /// Converts the currency to value fully in metal (ref)
    ///
    /// # Examples
    /// ```
    /// use pricing_tf::models::tf2_currency::Tf2Currency;
    /// let currency = Tf2Currency::new(2.0, 3.0); // 2 keys and 3 ref
    /// let exchange_rate = 60.0; // 1 key = 60 ref
    /// assert_eq!(currency.to_metal(exchange_rate), 123.0);
    /// ```
    pub fn to_metal(&self, exchange_rate: f64) -> f64 {
        self.metal + (self.keys * exchange_rate)
    }

    /// Converts the currency to value fully in keys
    ///
    /// # Examples
    /// ```
    /// use pricing_tf::models::tf2_currency::Tf2Currency;
    /// let currency = Tf2Currency::new(2.0, 30.0); // 2 keys and 30 ref
    /// let exchange_rate = 60.0; // 1 key = 60 ref
    /// assert_eq!(currency.to_keys(exchange_rate), 2.5);
    /// ```
    pub fn to_keys(&self, exchange_rate: f64) -> f64 {
        self.keys + (self.metal / exchange_rate)
    }
}
