from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS = Path(__file__).resolve().parents[2] / "tools" / "art"
sys.path.insert(0, str(TOOLS))

from complete_master_svg import (
    discover_edge,
    render_part_layers,
    render_svg,
    validate_authoring_svg,
)


def svg_text(parts: tuple[str, ...] = ("inner-body", "head-shell"), extra: str = "") -> str:
    groups = "".join(
        f'<g id="part-{part}" data-part-id="{part}"><rect x="100" y="100" width="20" height="20" fill="#fff"/></g>'
        for part in parts
    )
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" '
        'viewBox="0 0 1402 1122">'
        f"{groups}{extra}</svg>"
    )


class CompleteMasterSvgTests(unittest.TestCase):
    expected = ("inner-body", "head-shell")

    def validate(self, source: str) -> dict:
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "authoring.svg"
            path.write_text(source, encoding="utf-8")
            return validate_authoring_svg(path, self.expected)

    def test_accepts_exact_canvas_and_unique_part_groups(self):
        report = self.validate(svg_text())
        self.assertEqual([1402, 1122], report["canvas"])
        self.assertEqual(list(self.expected), report["partIds"])

    def test_rejects_missing_part(self):
        with self.assertRaisesRegex(ValueError, "missing.*head-shell"):
            self.validate(svg_text(("inner-body",)))

    def test_rejects_duplicate_part_id(self):
        with self.assertRaisesRegex(ValueError, "duplicate.*inner-body"):
            self.validate(svg_text(("inner-body", "inner-body", "head-shell")))

    def test_rejects_script_and_foreign_object(self):
        for forbidden in ("<script>alert(1)</script>", "<foreignObject/>"):
            with self.subTest(forbidden=forbidden):
                with self.assertRaisesRegex(ValueError, "forbidden element"):
                    self.validate(svg_text(extra=forbidden))

    def test_rejects_external_image_href(self):
        external = '<image href="https://example.invalid/reference.png" width="1" height="1"/>'
        with self.assertRaisesRegex(ValueError, "href"):
            self.validate(svg_text(extra=external))

    def test_rejects_remote_url_reference(self):
        remote = '<rect x="0" y="0" width="1" height="1" fill="url(https://example.invalid/a.svg#x)"/>'
        with self.assertRaisesRegex(ValueError, "remote URL"):
            self.validate(svg_text(extra=remote))

    def test_rejects_event_handler_attributes(self):
        dangerous = svg_text().replace("<svg ", '<svg onload="alert(1)" ')
        with self.assertRaisesRegex(ValueError, "event handler"):
            self.validate(dangerous)

    def test_rejects_wrong_canvas(self):
        with self.assertRaisesRegex(ValueError, "1402x1122"):
            self.validate(svg_text().replace('viewBox="0 0 1402 1122"', 'viewBox="0 0 1401 1122"'))

    def test_rejects_part_groups_nested_below_a_container(self):
        groups = "".join(
            f'<g id="part-{part}"><rect x="100" y="100" width="20" height="20"/></g>'
            for part in self.expected
        )
        nested = (
            '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" '
            f'viewBox="0 0 1402 1122"><g id="container">{groups}</g></svg>'
        )
        with self.assertRaisesRegex(ValueError, "top-level"):
            self.validate(nested)

    def test_renders_transparent_rgba_png_with_local_edge(self):
        edge = discover_edge()
        if edge is None:
            self.skipTest("Microsoft Edge is not installed")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.svg"
            output = root / "output.png"
            source.write_text(svg_text(), encoding="utf-8")
            render_svg(edge, source, output)
            with Image.open(output) as image:
                self.assertEqual((1402, 1122), image.size)
                self.assertEqual("RGBA", image.mode)
                self.assertEqual(0, image.getpixel((0, 0))[3])

    def test_part_layer_rendering_contains_only_the_requested_group(self):
        edge = discover_edge()
        if edge is None:
            self.skipTest("Microsoft Edge is not installed")
        source_text = (
            '<svg xmlns="http://www.w3.org/2000/svg" width="1402" height="1122" '
            'viewBox="0 0 1402 1122">'
            '<g id="part-inner-body"><rect x="100" y="100" width="20" height="20" fill="#fff"/></g>'
            '<g id="part-head-shell"><rect x="500" y="500" width="20" height="20" fill="#fff"/></g>'
            '</svg>'
        )
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "parts.svg"
            source.write_text(source_text, encoding="utf-8")
            outputs = render_part_layers(edge, source, root / "layers", self.expected)
            self.assertEqual(set(self.expected), set(outputs))
            with Image.open(outputs["inner-body"]) as inner:
                self.assertEqual((100, 100, 120, 120), inner.getchannel("A").getbbox())
            with Image.open(outputs["head-shell"]) as head:
                self.assertEqual((500, 500, 520, 520), head.getchannel("A").getbbox())


if __name__ == "__main__":
    unittest.main()
