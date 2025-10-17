use std::env;
use std::path::Path;

use anyhow::Result;

use convert_case::{Case, Casing};

fn generate<P1: AsRef<Path>, P2: AsRef<Path>>(crate_path: P1, path: P2) -> Result<()> {
    let sub_abbr = |str: String| -> String { str.replace("Ethercrab", "EtherCrab") };

    let to_pascal = |name: &str| -> String {
        let res = name.to_case(Case::Pascal);
        sub_abbr(res)
    };

    let to_class_name = |name: &str| {
        if name.split('-').count() == 1 {
            return "Base".to_string();
        }
        to_pascal(&name.replace("autd3capi-", ""))
    };

    let crate_name = crate_path.as_ref().file_name().unwrap().to_str().unwrap();
    let out_file = Path::new(path.as_ref()).join(format!("{}.cs", to_class_name(crate_name)));
    let dll_name = crate_name.replace('-', "_");
    let class_name = to_class_name(crate_name);

    glob::glob(&format!(
        "{}/**/*.rs",
        crate_path.as_ref().join("src").display()
    ))?
    .try_fold(csbindgen::Builder::default(), |acc, path| -> Result<_> {
        Ok(acc.input_extern_file(path?))
    })?
    .csharp_dll_name(dll_name)
    .csharp_class_name(format!("NativeMethods{}", class_name))
    .csharp_namespace("AUTD3Sharp.NativeMethods")
    .csharp_import_namespace("AUTD3Sharp.Utils")
    .csharp_import_namespace("AUTD3Sharp.Link")
    .csharp_import_namespace("System.Runtime.CompilerServices")
    .csharp_generate_const_filter(|_| true)
    .csharp_class_accessibility("internal")
    .always_included_types(["Status"])
    .generate_csharp_file(&out_file)
    .map_err(|_| anyhow::anyhow!("failed to generate cs wrapper"))?;

    let content = std::fs::read_to_string(&out_file)?;
    let content = content.replace("void*", "IntPtr");
    let content = content.replace("void @null", "byte @null");
    let content = content.replace("internal enum Status", "internal enum Status");
    let content = content.replace(
        "namespace AUTD3Sharp.NativeMethods",
        "[assembly: InternalsVisibleTo(\"tests\")]\nnamespace AUTD3Sharp.NativeMethods",
    );

    std::fs::write(&out_file, content)?;

    Ok(())
}

fn main() -> Result<()> {
    let home = env::var("CARGO_MANIFEST_DIR")?;
    {
        let path = format!(
            "{}/autd3-link-ethercrab/autd3-link-ethercrab/Cargo.toml",
            home
        );
        let crate_path = Path::new(&path).parent().unwrap();
        generate(&crate_path, "../../src/NativeMethods")?;
        generate(&crate_path, "../../unity/Assets/Scripts/NativeMethods")?;
    }
    {
        let path = format!("{}/autd3-capi-link-ethercrab/Cargo.toml", home);
        let crate_path = Path::new(&path).parent().unwrap();
        generate(&crate_path, "../../src/NativeMethods")?;
        generate(&crate_path, "../../unity/Assets/Scripts/NativeMethods")?;
    }
    Ok(())
}
