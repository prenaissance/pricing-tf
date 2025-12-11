use std::path::PathBuf;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let out_dir = PathBuf::from(std::env::var("OUT_DIR")?);
    tonic_prost_build::configure()
        .type_attribute(
            ".",
            "#[derive(serde::Deserialize)]\n#[serde(rename_all = \"snake_case\")]",
        )
        .extern_path(".google.protobuf.Timestamp", "::prost_wkt_types::Timestamp")
        .extern_path(".google.protobuf.Duration", "::prost_wkt_types::Duration")
        .build_server(true)
        .build_client(false)
        .file_descriptor_set_path(out_dir.join("pricing_tf_descriptor.bin"))
        .compile_protos(
            &[
                "protos/pricing_tf/pricing_service.proto",
                "protos/pricing_tf/block_user_service.proto",
            ],
            &["protos/pricing_tf", "google/protobuf"],
        )?;
    Ok(())
}
