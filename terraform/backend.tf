terraform {
  backend "s3" {
    bucket       = "terraform-jhyth"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
